using Equipo5.Api.Data;
using Microsoft.EntityFrameworkCore;

// Se conserva la consulta de clientes de la versión anterior.
public sealed class NorthwindService(ContextoDB db)
{
    public Task<List<CustomerResponse>> CustomersAsync(int limit, int offset, CancellationToken ct) =>
        db.Customers.AsNoTracking().OrderBy(c => c.CustomerId).Skip(offset).Take(limit)
            .Select(c => new CustomerResponse(c.CustomerId, c.CompanyName, c.ContactName, c.City, c.Country))
            .ToListAsync(ct);
}
public record CustomerResponse(string CustomerId, string CompanyName, string? ContactName, string? City, string? Country);
