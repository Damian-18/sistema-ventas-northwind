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
        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new("unique_name", user.Username),
            new("name", user.Username),
            new("role", user.Role),
            new("security_stamp", user.SecurityStamp),
            new("jti", Guid.NewGuid().ToString("N"))
        };
        if (user.CustomerId is not null)
            claims.Add(new Claim("customer_id", user.CustomerId));
        if (user.EmployeeId is int employeeId)
            claims.Add(new Claim("employee_id", employeeId.ToString()));

        var jwt = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"],
            claims, now, expires,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(jwt), "Bearer", expires, View(user));
    }

    public static UserView View(ApiUser user)
    {
        var now = DateTime.UtcNow;
        DateTime? lockoutEnd = user.LockoutEnd is DateTime value
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : null;
        return new(user.Id, user.Username, user.Role, user.IsActive, user.CreatedAtUtc,
            user.CustomerId, user.EmployeeId, user.FailedLoginAttempts, lockoutEnd,
            lockoutEnd is DateTime end && end > now);
    }
}
