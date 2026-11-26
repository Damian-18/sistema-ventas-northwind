using System.ComponentModel.DataAnnotations;
using Equipo5.Api.Contracts;
using Equipo5.Api.Data;
using Equipo5.Api.Infrastructure;
using Equipo5.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Equipo5.Api.Controllers;

[ApiController, Authorize(Roles = AppRoles.Admin), Route("api/empleados")]
public class EmployeesController(
    ContextoDB employees,
    UsuariosDB users,
    IPasswordHasher<ApiUser> passwordHasher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedEmployeesResponse>> Get(
        CancellationToken ct,
        [Range(1, 100)] int limit = 20,
        [Range(0, int.MaxValue)] int offset = 0,
        bool includeInactive = false)
    {
        var query = employees.Employees.AsNoTracking();
        if (!includeInactive)
            query = query.Where(employee => employee.IsActive);

        var totalCount = await query.CountAsync(ct);
        var page = await query.OrderBy(employee => employee.EmployeeId)
            .Skip(offset).Take(limit).ToListAsync(ct);
        var accountMap = await GetAccountMap(page.Select(employee => employee.EmployeeId), ct);
        var items = page.Select(employee =>
        {
            accountMap.TryGetValue(employee.EmployeeId, out var account);
            return new EmployeeListDto(
                employee.EmployeeId, employee.FirstName, employee.LastName, employee.Title,
                employee.HireDate, employee.City, employee.Country, employee.IsActive,
                account is not null, account?.Username, account?.Role, account?.IsActive);
        }).ToList();

        return Ok(new PagedEmployeesResponse(items, limit, offset, totalCount));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDetailDto>> GetById(int id, CancellationToken ct)
    {
        var employee = await employees.Employees.AsNoTracking()
            .SingleOrDefaultAsync(item => item.EmployeeId == id, ct);
        if (employee is null)
            return Problem("No se encontró el empleado solicitado.", statusCode: StatusCodes.Status404NotFound,
                title: "Empleado no encontrado");

        var user = await users.Users.AsNoTracking()
            .SingleOrDefaultAsync(item => item.EmployeeId == id, ct);
        var account = user is null
            ? null
            : new EmployeeAccount(id, user.Username, user.Role, user.IsActive);
        return Ok(ToDetail(employee, account));
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeDetailDto>> Create(CreateEmployeeRequest request, CancellationToken ct)
    {
        if (request.Role is not (AppRoles.Admin or AppRoles.Empleado))
            return Problem("El rol de la cuenta debe ser Admin o Empleado.",
                statusCode: StatusCodes.Status400BadRequest, title: "Rol inválido");

        var normalizedUsername = request.Username.ToUpperInvariant();
        if (await users.Users.AnyAsync(user => user.NormalizedUsername == normalizedUsername, ct))
            return Problem("El nombre de usuario ya existe.", statusCode: StatusCodes.Status409Conflict,
                title: "Usuario duplicado");
        if (await employees.Employees.AnyAsync(employee =>
                employee.FirstName == request.FirstName &&
                employee.LastName == request.LastName &&
                employee.BirthDate == request.BirthDate, ct))
            return Problem("Ya existe un empleado con esos nombres y fecha de nacimiento.",
                statusCode: StatusCodes.Status409Conflict, title: "Empleado duplicado");

        var employee = new Employee();
        request.Apply(employee);
        var user = new ApiUser
        {
            Username = request.Username,
            NormalizedUsername = normalizedUsername,
            Role = request.Role
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        await using var transaction = await employees.Database.BeginTransactionAsync(ct);
        await users.Database.UseTransactionAsync(transaction.GetDbTransaction(), ct);
        employees.Employees.Add(employee);
        await employees.SaveChangesAsync(ct);
        user.EmployeeId = employee.EmployeeId;
        users.Users.Add(user);
        await users.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = employee.EmployeeId },
            ToDetail(employee, new EmployeeAccount(employee.EmployeeId, user.Username, user.Role, user.IsActive)));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateEmployeeRequest request, CancellationToken ct)
    {
        var employee = await employees.Employees.FindAsync([id], ct);
        if (employee is null)
            return Problem("No se encontró el empleado solicitado.", statusCode: StatusCodes.Status404NotFound,
                title: "Empleado no encontrado");

        request.Apply(employee);
        await employees.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var employee = await employees.Employees.FindAsync([id], ct);
        if (employee is null)
            return Problem("No se encontró el empleado solicitado.", statusCode: StatusCodes.Status404NotFound,
                title: "Empleado no encontrado");

        await using var transaction = await employees.Database.BeginTransactionAsync(ct);
        await users.Database.UseTransactionAsync(transaction.GetDbTransaction(), ct);
        employee.IsActive = false;
        var account = await users.Users.SingleOrDefaultAsync(user => user.EmployeeId == id, ct);
        if (account is not null)
        {
            account.IsActive = false;
            account.SecurityStamp = Guid.NewGuid().ToString("N");
        }

        await employees.SaveChangesAsync(ct);
        if (account is not null)
            await users.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return NoContent();
    }

    private async Task<Dictionary<int, EmployeeAccount>> GetAccountMap(
        IEnumerable<int> employeeIds, CancellationToken ct)
    {
        var ids = employeeIds.ToArray();
        if (ids.Length == 0)
            return [];

        var accounts = await users.Users.AsNoTracking()
            .Where(user => user.EmployeeId.HasValue && ids.Contains(user.EmployeeId.Value))
            .ToListAsync(ct);
        return accounts.ToDictionary(
            user => user.EmployeeId.GetValueOrDefault(),
            user => new EmployeeAccount(
                user.EmployeeId.GetValueOrDefault(), user.Username, user.Role, user.IsActive));
    }

    private static EmployeeDetailDto ToDetail(Employee employee, EmployeeAccount? account) =>
        new(employee.EmployeeId, employee.FirstName, employee.LastName, employee.Title,
            employee.BirthDate, employee.HireDate, employee.Address, employee.City,
            employee.Country, employee.HomePhone, employee.ReportsTo, employee.IsActive,
            account is not null, account?.Username, account?.Role, account?.IsActive);

    private sealed record EmployeeAccount(int EmployeeId, string Username, string Role, bool IsActive);
}
