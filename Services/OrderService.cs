using System.Data;
using Equipo5.Api.Contracts;
using Equipo5.Api.Data;
using Equipo5.Api.Infrastructure;
using Equipo5.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Equipo5.Api.Services;

public sealed class OrderService(ContextoDB db)
{
    public async Task<OrderViewDto> CreateAsync(
        CreateOrderRequest request,
        string? customerId,
        int? employeeId,
        CancellationToken ct)
    {
        if (request.Items.Count == 0)
            throw new ApiProblemException(400, "Solicitud inválida", "La venta debe incluir al menos un producto.");

        if (request.Items.Any(item => item.Quantity <= 0 || item.Quantity > short.MaxValue))
            throw new ApiProblemException(400, "Solicitud inválida", "Las cantidades deben ser mayores que cero y no superar el límite permitido.");

        if (request.Items.Select(item => item.ProductId).Distinct().Count() != request.Items.Count)
            throw new ApiProblemException(400, "Solicitud inválida", "La venta no puede contener productos duplicados.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var customer = await db.Customers.SingleOrDefaultAsync(item => item.CustomerId == customerId, ct);
        if (customer is null)
            throw new ApiProblemException(404, "Cliente no encontrado", "No se encontró el cliente asociado a la venta.");
        if (!customer.IsActive)
            throw new ApiProblemException(409, "Cliente inactivo", "No se pueden registrar ventas para un cliente inactivo.");

        if (request.ShipVia is int shipperId &&
            !await db.Shippers.AnyAsync(shipper => shipper.ShipperId == shipperId, ct))
            throw new ApiProblemException(400, "Paquetería inválida", "La paquetería indicada no existe.");

        var productIds = request.Items.Select(item => item.ProductId).ToArray();
        var products = await db.Products
            .Where(product => productIds.Contains(product.ProductId))
            .ToDictionaryAsync(product => product.ProductId, ct);

        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                throw new ApiProblemException(404, "Producto no encontrado", $"No se encontró el producto {item.ProductId}.");
            if (product.Discontinued != 0)
                throw new ApiProblemException(409, "Producto descontinuado", $"El producto {item.ProductId} ya no está disponible para la venta.");
            if (product.UnitPrice is null || product.UnitsInStock is null)
                throw new ApiProblemException(409, "Datos de producto incompletos", $"El producto {item.ProductId} no tiene precio o stock disponible.");
            if (product.UnitsInStock < item.Quantity)
                throw new ApiProblemException(409, "Stock insuficiente", $"No hay stock suficiente para el producto {item.ProductId}.");
        }

        var order = new Order
        {
            CustomerId = customer.CustomerId,
            EmployeeId = employeeId,
            OrderDate = DateTime.UtcNow,
            ShipVia = request.ShipVia,
            Freight = 0,
            ShipAddress = request.ShipAddress,
            ShipCity = request.ShipCity,
            ShipCountry = request.ShipCountry
        };

        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            product.UnitsInStock = (short)(product.UnitsInStock!.Value - item.Quantity);
            order.OrderDetails.Add(new OrderDetail
            {
                ProductId = product.ProductId,
                Product = product,
                UnitPrice = product.UnitPrice!.Value,
                Quantity = (short)item.Quantity,
                Discount = 0
            });
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return ToView(order);
    }

    public static OrderSummaryDto ToSummary(Order order)
    {
        var subtotal = order.OrderDetails.Sum(detail =>
            detail.UnitPrice * detail.Quantity * (1m - (decimal)detail.Discount));
        var freight = order.Freight ?? 0m;
        return new OrderSummaryDto(
            order.OrderId, order.CustomerId, order.EmployeeId, order.OrderDate,
            order.ShipVia, subtotal, freight, subtotal + freight);
    }

    public static OrderViewDto ToView(Order order)
    {
        var details = order.OrderDetails.Select(detail =>
            new OrderDetailViewDto(
                detail.ProductId,
                detail.Product.ProductName,
                detail.UnitPrice,
                detail.Quantity,
                detail.Discount,
                detail.UnitPrice * detail.Quantity * (1m - (decimal)detail.Discount)))
            .ToList();

        return new OrderViewDto(
            ToSummary(order), order.ShipAddress, order.ShipCity, order.ShipCountry, details);
    }
}
