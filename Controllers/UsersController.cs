using System.ComponentModel.DataAnnotations;
using Equipo5.Api.Contracts;
using Equipo5.Api.Data;
using Equipo5.Api.Infrastructure;
using Equipo5.Api.Models;
using Equipo5.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Equipo5.Api.Controllers;

[ApiController, Authorize(Roles = AppRoles.Admin), Route("api/usuarios")]
public class UsersController(UsuariosDB db, IPasswordHasher<ApiUser> hasher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserView>>> Get(CancellationToken ct,
        [Range(1, 100)] int limit = 20, [Range(0, int.MaxValue)] int offset = 0) =>
        (await db.Users.AsNoTracking().OrderBy(u => u.Id).Skip(offset).Take(limit).ToListAsync(ct))
            .Select(TokenService.View).ToList();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserView>> GetById(int id, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, ct);
        return user is null ? NotFound() : Ok(TokenService.View(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserView>> Create(CreateUserRequest request, CancellationToken ct)
    {
        if (!AppRoles.IsValid(request.Role))
            return BadRequest(new { message = "El rol debe ser Admin, Empleado o Cliente." });

        var normalized = request.Username.ToUpperInvariant();
        if (await db.Users.AnyAsync(u => u.NormalizedUsername == normalized, ct)) return Conflict(new { message = "El nombre de usuario ya existe." });
        var user = new ApiUser { Username = request.Username, NormalizedUsername = normalized, Role = request.Role };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        db.Users.Add(user); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, TokenService.View(user));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateUserRequest request, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return NotFound();
        if (!AppRoles.IsValid(request.Role))
            return BadRequest(new { message = "El rol debe ser Admin, Empleado o Cliente." });
        if (id == CurrentId && (request.Role != AppRoles.Admin || request.IsActive != true))
            return Conflict(new { message = "No puedes quitarte el rol Admin ni desactivar tu propia cuenta." });
        var normalized = request.Username.ToUpperInvariant();
        if (await db.Users.AnyAsync(u => u.Id != id && u.NormalizedUsername == normalized, ct)) return Conflict(new { message = "El nombre de usuario ya existe." });
        user.Username = request.Username; user.NormalizedUsername = normalized;
        user.Role = request.Role; user.IsActive = request.IsActive!.Value;
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        if (id == CurrentId) return Conflict(new { message = "No puedes desactivar tu propia cuenta." });
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return NotFound();
        user.IsActive = false; user.SecurityStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpPut("{id:int}/password")]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return NotFound();
        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpPut("{id:int}/desbloquear")]
    public async Task<IActionResult> Unlock(int id, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return NotFound();
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private int CurrentId => int.Parse(User.FindFirst("sub")!.Value);
}
