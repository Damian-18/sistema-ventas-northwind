using System.Security.Cryptography;
using Equipo5.Api.Contracts;
using Equipo5.Api.Data;
using Equipo5.Api.Infrastructure;
using Equipo5.Api.Models;
using Equipo5.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Equipo5.Api.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(
    UsuariosDB db,
    ContextoDB contexto,
    IPasswordHasher<ApiUser> hasher,
    TokenService tokens,
    IConfiguration config) : ControllerBase
{
    // Evita una respuesta mucho más rápida para nombres inexistentes.
    private static readonly string DummyHash = new PasswordHasher<ApiUser>().HashPassword(new ApiUser(), Guid.NewGuid().ToString());

    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var normalized = request.Username.Trim().ToUpperInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedUsername == normalized, ct);
        var now = DateTime.UtcNow;
        if (user?.LockoutEnd is DateTime lockoutEnd && lockoutEnd > now)
            return Locked(lockoutEnd, now);

        var result = hasher.VerifyHashedPassword(user ?? new ApiUser(), user?.PasswordHash ?? DummyHash, request.Password);
        if (user is null || !user.IsActive)
            return Unauthorized(new { message = "Usuario o contraseña incorrectos." });

        if (result == PasswordVerificationResult.Failed)
        {
            user.FailedLoginAttempts++;
            var maxAttempts = config.GetValue<int>("Security:MaxFailedLoginAttempts");
            if (user.FailedLoginAttempts >= maxAttempts)
            {
                var lockoutMinutes = config.GetValue<int>("Security:LockoutMinutes");
                user.LockoutEnd = now.AddMinutes(lockoutMinutes);
                await db.SaveChangesAsync(ct);
                return Locked(user.LockoutEnd.Value, now);
            }

            await db.SaveChangesAsync(ct);
            return Unauthorized(new { message = "Usuario o contraseña incorrectos." });
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = hasher.HashPassword(user, request.Password);
        await db.SaveChangesAsync(ct);
        return Ok(tokens.Create(user));
    }

    [HttpPost("register"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<ActionResult<UserView>> Register(RegisterRequest request, CancellationToken ct)
    {
        var normalized = request.Username.Trim().ToUpperInvariant();
        if (await db.Users.AnyAsync(u => u.NormalizedUsername == normalized, ct))
            return Conflict(new { message = "El nombre de usuario ya existe." });

        var customerId = "";
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var candidate = GenerateCustomerId();
            if (!await contexto.Customers.AnyAsync(c => c.CustomerId == candidate, ct))
            {
                customerId = candidate;
                break;
            }
        }

        if (customerId.Length == 0)
            return Conflict(new { message = "No se pudo asignar un identificador de cliente. Inténtalo de nuevo." });

        var customer = new Customer
        {
            CustomerId = customerId,
            CompanyName = request.CompanyName,
            ContactName = request.ContactName,
            Phone = request.Phone,
            Address = request.Address,
            City = request.City,
            Country = request.Country,
            IsActive = true
        };
        var user = new ApiUser
        {
            Username = request.Username,
            NormalizedUsername = normalized,
            Role = AppRoles.Cliente,
            CustomerId = customerId,
            IsActive = true
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        await using var transaction = await contexto.Database.BeginTransactionAsync(ct);
        await db.Database.UseTransactionAsync(transaction.GetDbTransaction(), ct);
        contexto.Customers.Add(customer);
        await contexto.SaveChangesAsync(ct);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return StatusCode(StatusCodes.Status201Created, TokenService.View(user));
    }

    [HttpGet("me"), Authorize]
    public async Task<ActionResult<UserView>> Me(CancellationToken ct) =>
        Ok(TokenService.View(await Current(ct)));

    [HttpPut("password"), Authorize, EnableRateLimiting("login")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await Current(ct);
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            return BadRequest(new { message = "La contraseña actual es incorrecta." });
        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var user = await Current(ct); user.SecurityStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(ct); return NoContent();
    }
    private Task<ApiUser> Current(CancellationToken ct) => db.Users.SingleAsync(u => u.Id == int.Parse(User.FindFirst("sub")!.Value), ct);

    private ObjectResult Locked(DateTime lockoutEnd, DateTime now)
    {
        var minutesRemaining = Math.Max(1, (int)Math.Ceiling((lockoutEnd - now).TotalMinutes));
        var unit = minutesRemaining == 1 ? "minuto" : "minutos";
        return StatusCode(StatusCodes.Status423Locked,
            new { message = $"La cuenta está bloqueada. Inténtalo de nuevo en aproximadamente {minutesRemaining} {unit}." });
    }

    private static string GenerateCustomerId()
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var characters = new char[5];
        for (var index = 0; index < characters.Length; index++)
            characters[index] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        return new string(characters);
    }
}
