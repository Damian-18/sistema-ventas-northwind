using System.ComponentModel.DataAnnotations;

namespace Equipo5.Api.Contracts;

public class CreateOrderRequest
{
    [StringLength(5, MinimumLength = 5)]
    public string? CustomerId { get; set; }

    [StringLength(60)]
    public string? ShipAddress { get; set; }

    [StringLength(15)]
    public string? ShipCity { get; set; }

    [StringLength(15)]
    public string? ShipCountry { get; set; }

    [Range(1, int.MaxValue)]
    public int? ShipVia { get; set; }

    [Required, MinLength(1)]
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}

public class CreateOrderItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, short.MaxValue)]
    public int Quantity { get; set; }
}

public record OrderSummaryDto(
    int OrderId,
    string? CustomerId,
    int? EmployeeId,
    DateTime? OrderDate,
    int? ShipVia,
    decimal Subtotal,
    decimal Freight,
    decimal Total);

public record OrderDetailViewDto(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    short Quantity,
    float Discount,
    decimal Subtotal);

public record OrderViewDto(
    OrderSummaryDto Summary,
    string? ShipAddress,
    string? ShipCity,
    string? ShipCountry,
    IReadOnlyList<OrderDetailViewDto> Details);

public record PagedOrdersResponse(
    IReadOnlyList<OrderSummaryDto> Items,
    int Limit,
    int Offset,
    int TotalCount);
