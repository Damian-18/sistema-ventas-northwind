using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Equipo5.Api.Contracts;
using Equipo5.Api.Data;
using Equipo5.Api.Infrastructure;
using Equipo5.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Equipo5.Api.Controllers;

[ApiController, Route("api/clientes")]
public class CustomersController(ContextoDB customers, UsuariosDB users) : ControllerBase
{
    [HttpGet, Authorize(Roles = AppRoles.Admin + "," + AppRoles.Empleado)]
    public async Task<ActionResult<PagedCustomersResponse>> Get(
        CancellationToken ct,
        [Range(1, 100)] int limit = 20,
        [Range(0, int.MaxValue)] int offset = 0,
        bool includeInactive = false,
        string? search = null)
    {
        var query = customers.Customers.AsNoTracking();
        if (!includeInactive)
            query = query.Where(customer => customer.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(customer =>
                customer.CompanyName.Contains(term) ||
                (customer.ContactName != null && customer.ContactName.Contains(term)) ||
                (customer.City != null && customer.City.Contains(term)));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderBy(customer => customer.CustomerId)
            .Skip(offset).Take(limit)
            .Select(customer => new CustomerListDto(
                customer.CustomerId, customer.CompanyName, customer.ContactName,
                customer.City, customer.Country, customer.IsActive))
            .ToListAsync(ct);
        return Ok(new PagedCustomersResponse(items, limit, offset, totalCount));
    }

    [HttpGet("{id}"), Authorize(Roles = AppRoles.Admin + "," + AppRoles.Empleado + "," + AppRoles.Cliente)]
    public async Task<ActionResult<CustomerDetailDto>> GetById(string id, CancellationToken ct)
    {
        if (!OwnsCustomer(id))
            return Forbid();

        var customer = await customers.Customers.AsNoTracking()
            .Where(item => item.CustomerId == id)
            .Select(item => new CustomerDetailDto(
                item.CustomerId, item.CompanyName, item.ContactName, item.ContactTitle,
                item.Address, item.City, item.Region, item.PostalCode, item.Country,
                item.Phone, item.Fax, item.IsActive))
            .SingleOrDefaultAsync(ct);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost, Authorize(Roles = AppRoles.Admin + "," + AppRoles.Empleado)]
    public async Task<ActionResult<CustomerDetailDto>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        if (await customers.Customers.AnyAsync(customer => customer.CompanyName == request.CompanyName, ct))
            return Conflict(new { message = "Ya existe un cliente con ese nombre de empresa." });

        var customerId = request.CustomerId;
        if (customerId is null)
        {
            customerId = await GenerateCustomerId(ct);
            if (customerId is null)
                return Conflict(new { message = "No se pudo asignar un identificador de cliente. Inténtalo de nuevo." });
        }
        else if (await customers.Customers.AnyAsync(customer => customer.CustomerId == customerId, ct))
        {
            return Conflict(new { message = "El identificador de cliente ya existe." });
        }

        var customer = new Customer { CustomerId = customerId };
        request.Apply(customer);
        customer.IsActive = true;
        customers.Customers.Add(customer);
        await customers.SaveChangesAsync(ct);
        var result = ToDetail(customer);
        return CreatedAtAction(nameof(GetById), new { id = customer.CustomerId }, result);
    }

    [HttpPut("{id}"), Authorize(Roles = AppRoles.Admin + "," + AppRoles.Empleado + "," + AppRoles.Cliente)]
    public async Task<IActionResult> Update(string id, UpdateCustomerRequest request, CancellationToken ct)
    {
        if (!OwnsCustomer(id))
            return Forbid();

        var customer = await customers.Customers.FindAsync([id], ct);
        if (customer is null)
            return NotFound();

        request.Apply(customer);
        await customers.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id}"), Authorize(Roles = AppRoles.Admin + "," + AppRoles.Empleado)]
    public async Task<IActionResult> Deactivate(string id, CancellationToken ct)
    {
        var customer = await customers.Customers.FindAsync([id], ct);
        if (customer is null)
            return NotFound();

        await using var transaction = await customers.Database.BeginTransactionAsync(ct);
        await users.Database.UseTransactionAsync(transaction.GetDbTransaction(), ct);
        customer.IsActive = false;
        var account = await users.Users.SingleOrDefaultAsync(user => user.CustomerId == id, ct);
        if (account is not null)
        {
            account.IsActive = false;
            account.SecurityStamp = Guid.NewGuid().ToString("N");
        }

        await customers.SaveChangesAsync(ct);
        if (account is not null)
            await users.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return NoContent();
    }

    [HttpPut("{id}/reactivar"), Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Reactivate(string id, CancellationToken ct)
    {
        var customer = await customers.Customers.FindAsync([id], ct);
        if (customer is null)
            return NotFound();

        await using var transaction = await customers.Database.BeginTransactionAsync(ct);
        await users.Database.UseTransactionAsync(transaction.GetDbTransaction(), ct);
        customer.IsActive = true;
        var account = await users.Users.SingleOrDefaultAsync(user => user.CustomerId == id, ct);
        if (account is not null)
        {
            account.IsActive = true;
            account.SecurityStamp = Guid.NewGuid().ToString("N");
        }

        await customers.SaveChangesAsync(ct);
        if (account is not null)
            await users.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return NoContent();
    }

    private bool OwnsCustomer(string customerId) =>
        !User.IsInRole(AppRoles.Cliente) ||
        string.Equals(User.FindFirst("customer_id")?.Value, customerId, StringComparison.Ordinal);

    private async Task<string?> GenerateCustomerId(CancellationToken ct)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var characters = new char[5];
            for (var index = 0; index < characters.Length; index++)
                characters[index] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
            var candidate = new string(characters);
            if (!await customers.Customers.AnyAsync(customer => customer.CustomerId == candidate, ct))
                return candidate;
        }

        return null;
    }

    private static CustomerDetailDto ToDetail(Customer customer) =>
        new(customer.CustomerId, customer.CompanyName, customer.ContactName, customer.ContactTitle,
            customer.Address, customer.City, customer.Region, customer.PostalCode, customer.Country,
            customer.Phone, customer.Fax, customer.IsActive);
}
