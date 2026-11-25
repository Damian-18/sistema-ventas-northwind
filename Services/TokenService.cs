using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Equipo5.Api.Contracts;
using Equipo5.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace Equipo5.Api.Services;

public class TokenService(IConfiguration config)
{
    public LoginResponse Create(ApiUser user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(config.GetValue<int>("Jwt:ExpirationMinutes"));
        var jwt = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"],
            [new Claim("sub", user.Id.ToString()), new Claim("name", user.Username),
             new Claim("role", user.Role), new Claim("stamp", user.SecurityStamp),
             new Claim("jti", Guid.NewGuid().ToString("N"))], now, expires,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(jwt), "Bearer", expires, View(user));
    }
    public static UserView View(ApiUser user) => new(user.Id, user.Username, user.Role, user.IsActive, user.CreatedAtUtc);
}
