using System.ComponentModel.DataAnnotations;
using Equipo5.Api.Contracts;
using Equipo5.Api.Data;
using Equipo5.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Equipo5.Api.Controllers;

[ApiController, Authorize, Route("api/proveedores")]
public class SuppliersController(ContextoDB db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SupplierView>>> Get(CancellationToken ct,
        [Range(1, 100)] int limit = 20, [Range(0, int.MaxValue)] int offset = 0) =>
        await db.Suppliers.AsNoTracking().OrderBy(s => s.SupplierId).Skip(offset).Take(limit).Select(SupplierView.Select).ToListAsync(ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SupplierView>> GetById(int id, CancellationToken ct)
    {
        var item = await db.Suppliers.AsNoTracking().Where(s => s.SupplierId == id).Select(SupplierView.Select).SingleOrDefaultAsync(ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost, Authorize(Policy = "WriteCatalog")]
    public async Task<ActionResult<SupplierView>> Create(SupplierInput input, CancellationToken ct)
    {
        var companyName = input.CompanyName.Trim();
        if (await db.Suppliers.AnyAsync(supplier => supplier.CompanyName == companyName, ct))
            return Problem("Ya existe un proveedor con ese nombre de empresa.", statusCode: 409, title: "Proveedor duplicado");
        var item = new Supplier(); input.Apply(item); db.Suppliers.Add(item); await db.SaveChangesAsync(ct);
        var result = await db.Suppliers.Where(s => s.SupplierId == item.SupplierId).Select(SupplierView.Select).SingleAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = item.SupplierId }, result);
    }

    [HttpPut("{id:int}"), Authorize(Policy = "WriteCatalog")]
    public async Task<IActionResult> Update(int id, SupplierInput input, CancellationToken ct)
    {
        var item = await db.Suppliers.FindAsync([id], ct);
        if (item is null) return NotFound();
        var companyName = input.CompanyName.Trim();
        if (await db.Suppliers.AnyAsync(supplier => supplier.SupplierId != id && supplier.CompanyName == companyName, ct))
            return Problem("Ya existe un proveedor con ese nombre de empresa.", statusCode: 409, title: "Proveedor duplicado");
        input.Apply(item); await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize(Policy = "WriteCatalog")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var item = await db.Suppliers.FindAsync([id], ct);
        if (item is null) return NotFound();
        if (await db.Products.AnyAsync(p => p.SupplierId == id, ct)) return Conflict(new { message = "El proveedor tiene productos asociados." });
        db.Suppliers.Remove(item); await db.SaveChangesAsync(ct); return NoContent();
    }
}
