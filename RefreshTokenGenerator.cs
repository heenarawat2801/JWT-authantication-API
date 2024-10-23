using System.Security.Cryptography;
using CustomerAPI.Models;
using Microsoft.AspNetCore.SignalR;

namespace CustomerAPI
{
    public class RefreshTokenGenerator: IRefreshTokenGenerator
    {
        private readonly LearnDbContext _context;

        public RefreshTokenGenerator(LearnDbContext learnDb)
        { 
            _context = learnDb;
        }

        public string GenerateToken(string username)
        {
            var randomnumber = new byte[32];
            using(var randomnumbergenerator = RandomNumberGenerator.Create())
            {
                randomnumbergenerator.GetBytes(randomnumber);
                string RefreshToken = Convert.ToBase64String(randomnumber);

                var _user = _context.TblRefreshtokens.FirstOrDefault(o => o.UserId == username);
                if (_user != null)
                {
                    _user.RefreshToken = RefreshToken;
                    _context.SaveChanges();
                }
                else
                {
                    TblRefreshtoken tblRefreshtoken = new TblRefreshtoken()
                    {
                        UserId = username,
                        TokenId = new Random().Next().ToString(),
                        RefreshToken = RefreshToken,
                        IsActive = true
                    };

                }

                return RefreshToken;
            }
        }
    }
}
