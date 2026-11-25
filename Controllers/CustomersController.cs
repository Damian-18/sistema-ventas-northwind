using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace Equipo5.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/clientes")]
public class CustomersController(NorthwindService db, ILogger<CustomersController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<CustomerResponse>>(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(503)]
    public async Task<IActionResult> Get(CancellationToken ct, int limit = 20, int offset = 0)
    {
        if (limit is < 1 or > 100 || offset < 0)
            return BadRequest(new { message = "limit debe estar entre 1 y 100; offset debe ser >= 0." });
        try { return Ok(await db.CustomersAsync(limit, offset, ct)); }
        catch (MySqlException ex)
        {
            logger.LogError(ex, "No se pudieron consultar Customers.");
            return Problem(statusCode: 503, title: "Northwind no disponible", detail: "Equipo 5 debe revisar la conexión y el esquema de MySQL.");
        }
    }
}
