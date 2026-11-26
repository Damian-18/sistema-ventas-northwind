using System.ComponentModel.DataAnnotations;
using Equipo5.Api.Contracts;
using Equipo5.Api.Data;
using Equipo5.Api.Infrastructure;
using Equipo5.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Equipo5.Api.Controllers;

[ApiController, Authorize, Route("api/productos")]
public class ProductsController(ContextoDB db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ProductView>>> Get(CancellationToken ct,
        [Range(1, 100)] int limit = 20, [Range(0, int.MaxValue)] int offset = 0)
    {
        var query = db.Products.AsNoTracking();
        if (User.IsInRole(AppRoles.Cliente))
            query = query.Where(product => product.Discontinued == 0);

        return await query.OrderBy(p => p.ProductId).Skip(offset).Take(limit).Select(ProductView.Select).ToListAsync(ct);
    }

    [HttpGet]
    [Route("paginados")]
    public async Task<ActionResult<PagedProductsResponse>> GetPaged(
        [FromQuery] string? search = null, [FromQuery] int p = 1, [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        p = p < 1 ? 1 : p;
        pageSize = pageSize < 1 ? 12 : Math.Min(pageSize, 48);

        var products = db.Products.AsNoTracking();
        if (User.IsInRole(AppRoles.Cliente))
            products = products.Where(product => product.Discontinued == 0);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLowerInvariant();
            products = products.Where(product =>
                product.ProductName.ToLower().Contains(searchTerm) ||
                (product.QuantityPerUnit != null && product.QuantityPerUnit.ToLower().Contains(searchTerm)));
        }

        var totalCount = await products.CountAsync(ct);
        var offset = Math.Min((long)(p - 1) * pageSize, int.MaxValue);
        var items = await products
            .OrderBy(product => product.ProductId)
            .Select(product => new ProductCardDto(
                product.ProductId,
                product.ProductName,
                product.QuantityPerUnit,
                product.UnitPrice,
                product.UnitsInStock,
                product.Category == null ? null : product.Category.CategoryName))
            .Skip((int)offset)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(new PagedProductsResponse(items, p, pageSize, totalCount,
            (long)p * pageSize < totalCount));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductView>> GetById(int id, CancellationToken ct)
    {
        var query = db.Products.AsNoTracking().Where(p => p.ProductId == id);
        if (User.IsInRole(AppRoles.Cliente))
            query = query.Where(product => product.Discontinued == 0);
        var item = await query.Select(ProductView.Select).SingleOrDefaultAsync(ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost, Authorize(Policy = "WriteCatalog")]
    public async Task<ActionResult<ProductView>> Create(ProductInput input, CancellationToken ct)
    {
        if (await ValidateReferences(input, ct) is { } error) return BadRequest(new { message = error });
        var productName = input.ProductName.Trim();
        if (await db.Products.AnyAsync(product => product.ProductName == productName, ct))
            return Problem("Ya existe un producto con ese nombre.", statusCode: 409, title: "Producto duplicado");
        var item = new Product(); input.Apply(item); db.Products.Add(item); await db.SaveChangesAsync(ct);
        var result = await db.Products.Where(p => p.ProductId == item.ProductId).Select(ProductView.Select).SingleAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = item.ProductId }, result);
    }

    [HttpPut("{id:int}"), Authorize(Policy = "WriteCatalog")]
    public async Task<IActionResult> Update(int id, ProductInput input, CancellationToken ct)
    {
        var item = await db.Products.FindAsync([id], ct);
        if (item is null) return NotFound();
        if (await ValidateReferences(input, ct) is { } error) return BadRequest(new { message = error });
        var productName = input.ProductName.Trim();
        if (await db.Products.AnyAsync(product => product.ProductId != id && product.ProductName == productName, ct))
            return Problem("Ya existe un producto con ese nombre.", statusCode: 409, title: "Producto duplicado");
        input.Apply(item); await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize(Policy = "WriteCatalog")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var item = await db.Products.FindAsync([id], ct);
        if (item is null) return NotFound();
        item.Discontinued = 1;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<string?> ValidateReferences(ProductInput input, CancellationToken ct)
    {
        if (input.UnitPrice is decimal price && decimal.Round(price, 4) != price) return "unitPrice admite hasta 4 decimales.";
        if (input.CategoryId is int category && !await db.Categories.AnyAsync(c => c.CategoryId == category, ct)) return "La categoría no existe.";
        if (input.SupplierId is int supplier && !await db.Suppliers.AnyAsync(s => s.SupplierId == supplier, ct)) return "El proveedor no existe.";
        return null;
    }
}
