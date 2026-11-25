using Equipo5.Api.Contracts;
using Equipo5.Api.Data;
using Equipo5.Api.Models;
using Equipo5.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Equipo5.Api.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(UsuariosDB db, IPasswordHasher<ApiUser> hasher, TokenService tokens) : ControllerBase
{
    // Evita una respuesta mucho más rápida para nombres inexistentes.
    private static readonly string DummyHash = new PasswordHasher<ApiUser>().HashPassword(new ApiUser(), Guid.NewGuid().ToString());

    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var normalized = request.Username.Trim().ToUpperInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedUsername == normalized, ct);
        var result = hasher.VerifyHashedPassword(user ?? new ApiUser(), user?.PasswordHash ?? DummyHash, request.Password);
        if (user is null || !user.IsActive || result == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Usuario o contraseña incorrectos." });
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            await db.SaveChangesAsync(ct);
        }
        return Ok(tokens.Create(user));
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
}
