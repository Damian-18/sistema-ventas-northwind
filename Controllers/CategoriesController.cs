using System.ComponentModel.DataAnnotations;
using Equipo5.Api.Contracts;
using Equipo5.Api.Data;
using Equipo5.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Equipo5.Api.Controllers;

[ApiController, Authorize, Route("api/categorias")]
public class CategoriesController(ContextoDB db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryView>>> Get(CancellationToken ct,
        [Range(1, 100)] int limit = 20, [Range(0, int.MaxValue)] int offset = 0) =>
        await db.Categories.AsNoTracking().OrderBy(c => c.CategoryId).Skip(offset).Take(limit).Select(CategoryView.Select).ToListAsync(ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryView>> GetById(int id, CancellationToken ct)
    {
        var item = await db.Categories.AsNoTracking().Where(c => c.CategoryId == id).Select(CategoryView.Select).SingleOrDefaultAsync(ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost, Authorize(Policy = "WriteCatalog")]
    public async Task<ActionResult<CategoryView>> Create(CategoryInput input, CancellationToken ct)
    {
        var item = new Category(); input.Apply(item); db.Categories.Add(item); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = item.CategoryId }, new CategoryView(item.CategoryId, item.CategoryName, item.Description, item.Picture));
    }

    [HttpPut("{id:int}"), Authorize(Policy = "WriteCatalog")]
    public async Task<IActionResult> Update(int id, CategoryInput input, CancellationToken ct)
    {
        var item = await db.Categories.FindAsync([id], ct);
        if (item is null) return NotFound();
        input.Apply(item); await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize(Policy = "WriteCatalog")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var item = await db.Categories.FindAsync([id], ct);
        if (item is null) return NotFound();
        if (await db.Products.AnyAsync(p => p.CategoryId == id, ct)) return Conflict(new { message = "La categoría tiene productos asociados." });
        db.Categories.Remove(item); await db.SaveChangesAsync(ct); return NoContent();
    }
}
