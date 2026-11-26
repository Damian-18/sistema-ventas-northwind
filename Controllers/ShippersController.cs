using Equipo5.Api.Contracts;
using Equipo5.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Equipo5.Api.Controllers;

[ApiController, Authorize, Route("api/shippers")]
public class ShippersController(ContextoDB db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ShipperView>>> Get(CancellationToken ct) =>
        await db.Shippers.AsNoTracking()
            .OrderBy(shipper => shipper.ShipperId)
            .Select(shipper => new ShipperView(shipper.ShipperId, shipper.CompanyName))
            .ToListAsync(ct);
}
