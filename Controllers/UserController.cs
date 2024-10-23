using CustomerAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
using System.Text;

namespace CustomerAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly LearnDbContext _context;   
        private readonly JWTSetting _jwtSetting;
        private readonly IRefreshTokenGenerator tokenGenerator;
        public UserController(LearnDbContext learnDb, IOptions<JWTSetting> options, IRefreshTokenGenerator refreshToken) 
        { 
            _context = learnDb;
            _jwtSetting = options.Value;
            tokenGenerator = refreshToken;
        }
        public TokenResponse Authenticate(string username, Claim[] claims)
        {
            TokenResponse tokenResponse = new TokenResponse();
            var tokenkey = Encoding.UTF8.GetBytes(_jwtSetting.securitykey);
            var tokenhandler = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddMinutes(10),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(tokenkey), SecurityAlgorithms.HmacSha256)
            );
            tokenResponse.JWTToken = new JwtSecurityTokenHandler().WriteToken(tokenhandler);
            tokenResponse.RefreshToken = tokenGenerator.GenerateToken(username);
            return tokenResponse;
        }

        [Route("Authenticate")]
        [HttpPost]
        public IActionResult Authenticate([FromBody] usercred user)
        {
            TokenResponse tokenResponse = new TokenResponse();

            var _user = _context.TblUsers.FirstOrDefault(o => o.Userid == user.username && o.Password == user.password);
            if (_user == null)
            {
                return Unauthorized();
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenkey = Encoding.UTF8.GetBytes(_jwtSetting.securitykey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                new Claim[]
                {
                new Claim(ClaimTypes.Name, _user.Userid),
                new Claim(ClaimTypes.Role, _user.Role)
                }
                ),
                Expires = DateTime.Now.AddMinutes(10),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenkey), SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            string finaltoken = tokenHandler.WriteToken(token);

            tokenResponse.JWTToken = finaltoken;
            tokenResponse.RefreshToken = tokenGenerator.GenerateToken(user.username);
            return Ok(tokenResponse);
        }
        [Route("Refresh")]
        [HttpPost]
        public IActionResult Refresh([FromBody] TokenResponse token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principle = tokenHandler.ValidateToken(token.JWTToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSetting.securitykey)),
                ValidateIssuer = false, // Set to true if you want to validate the issuer
                ValidateAudience = false
            }, out securityToken);

            var _token = securityToken as JwtSecurityToken;
            if (_token != null && !_token.Header.Alg.Equals(SecurityAlgorithms.HmacSha256)) 
            {
                return Unauthorized();
            }

            var username = principle.Identity.Name;
            var reftable = _context.TblRefreshtokens.FirstOrDefault(o => o.UserId == username && o.RefreshToken == token.RefreshToken);

            if (reftable == null) 
            {
                return Unauthorized();
            }

            TokenResponse _result = Authenticate(username, principle.Claims.ToArray());

            return Ok(_result);
        }
    }
}
