using Microsoft.EntityFrameworkCore;
using CustomerAPI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CustomerAPI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<LearnDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("constring"), sqlOptions =>
        sqlOptions.CommandTimeout(300))); // Timeout in seconds

var _dbContext = builder.Services.BuildServiceProvider().GetService<LearnDbContext>();

builder.Services.AddSingleton<IRefreshTokenGenerator>(provider => new RefreshTokenGenerator(_dbContext));

// Configure JWT settings
builder.Services.Configure<JWTSetting>(builder.Configuration.GetSection("JWTSetting"));

// Retrieve the JWT settings
var jwtSettings = builder.Configuration.GetSection("JWTSetting").Get<JWTSetting>();

if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.securitykey))
{
    throw new ArgumentNullException("JWTSetting:securitykey", "JWT security key is not set in the configuration.");
}

var key = Encoding.UTF8.GetBytes(jwtSettings.securitykey);

// Configure authentication services
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Set to true in production
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false, // Set to true if you want to validate the issuer
            ValidateAudience = false // Set to true if you want to validate the audience
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication(); // Ensure this is before UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();