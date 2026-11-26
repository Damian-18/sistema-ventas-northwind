using System.ComponentModel.DataAnnotations;
using Equipo5.Api.Contracts;
using Equipo5.Api.Data;
using Equipo5.Api.Infrastructure;
using Equipo5.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Equipo5.Api.Controllers;

[ApiController, Authorize(Roles = AppRoles.Admin + "," + AppRoles.Empleado + "," + AppRoles.Cliente), Route("api/ventas")]
public class OrdersController(ContextoDB db, OrderService orderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedOrdersResponse>> Get(
        CancellationToken ct,
        [Range(1, 100)] int limit = 20,
        [Range(0, int.MaxValue)] int offset = 0,
        string? customerId = null,
        DateTime? from = null,
        DateTime? to = null)
    {
        if (from is not null && to is not null && from > to)
            return Problem("La fecha inicial no puede ser posterior a la fecha final.", statusCode: 400, title: "Rango de fechas inválido");

        var query = db.Orders.AsNoTracking().AsQueryable();
        if (User.IsInRole(AppRoles.Cliente))
        {
            var ownerId = User.FindFirst("customer_id")?.Value;
            if (string.IsNullOrWhiteSpace(ownerId))
                return Problem("La sesión de cliente no tiene un vínculo válido.",
                    statusCode: StatusCodes.Status403Forbidden, title: "Acceso denegado");
            query = query.Where(order => order.CustomerId == ownerId);
        }
        else if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(order => order.CustomerId == customerId);
        }

        if (from is DateTime start)
            query = query.Where(order => order.OrderDate >= start);
        if (to is DateTime end)
            query = query.Where(order => order.OrderDate <= end);

        var totalCount = await query.CountAsync(ct);
        var orders = await query
            .OrderByDescending(order => order.OrderDate)
            .ThenByDescending(order => order.OrderId)
            .Skip(offset)
            .Take(limit)
            .Include(order => order.OrderDetails)
            .ToListAsync(ct);

        return Ok(new PagedOrdersResponse(
            orders.Select(OrderService.ToSummary).ToList(), limit, offset, totalCount));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderViewDto>> GetById(int id, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(item => item.OrderDetails)
            .ThenInclude(detail => detail.Product)
            .SingleOrDefaultAsync(item => item.OrderId == id, ct);
        if (order is null)
            return Problem("No se encontró la venta solicitada.", statusCode: StatusCodes.Status404NotFound,
                title: "Venta no encontrada");

        if (User.IsInRole(AppRoles.Cliente))
        {
            var ownerId = User.FindFirst("customer_id")?.Value;
            if (string.IsNullOrWhiteSpace(ownerId) ||
                !string.Equals(ownerId, order.CustomerId, StringComparison.Ordinal))
                return Problem("No puedes consultar la venta de otro cliente.",
                    statusCode: StatusCodes.Status403Forbidden, title: "Acceso denegado");
        }

        return Ok(OrderService.ToView(order));
    }

    [HttpPost]
    public async Task<ActionResult<OrderViewDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var isCustomer = User.IsInRole(AppRoles.Cliente);
        var customerId = isCustomer
            ? User.FindFirst("customer_id")?.Value
            : request.CustomerId;
        if (string.IsNullOrWhiteSpace(customerId))
            return Problem("Debes indicar un cliente para la venta.", statusCode: StatusCodes.Status400BadRequest,
                title: "Cliente requerido");
        if (!isCustomer && request.CustomerId is null)
            return Problem("Debes indicar un cliente para la venta.", statusCode: StatusCodes.Status400BadRequest,
                title: "Cliente requerido");

        int? employeeId = null;
        if (!isCustomer && User.FindFirst("employee_id") is { } employeeClaim)
        {
            if (!int.TryParse(employeeClaim.Value, out var parsedEmployeeId))
                return Problem("La sesión no tiene un vínculo válido con un empleado.",
                    statusCode: StatusCodes.Status403Forbidden, title: "Acceso denegado");
            employeeId = parsedEmployeeId;
        }

        var order = await orderService.CreateAsync(request, customerId, employeeId, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Summary.OrderId }, order);
    }
}
