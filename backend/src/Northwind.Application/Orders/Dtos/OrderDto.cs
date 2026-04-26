namespace Northwind.Application.Orders.Dtos;

/// <summary>
/// Read-only projection of an Order for API responses.
/// Flattens the aggregate into a shape that's convenient for the frontend.
/// </summary>
public sealed record OrderDto
{
    public int Id { get; init; }
    public string CustomerId { get; init; } = string.Empty;
    public int EmployeeId { get; init; }
    public int? ShipperId { get; init; }
    public DateTime OrderDate { get; init; }
    public DateTime? RequiredDate { get; init; }
    public DateTime? ShippedDate { get; init; }
    public bool IsShipped { get; init; }
    public decimal Freight { get; init; }
    public string ShipName { get; init; } = string.Empty;
    public string ShipStreet { get; init; } = string.Empty;
    public string ShipCity { get; init; } = string.Empty;
    public string? ShipRegion { get; init; }
    public string? ShipPostalCode { get; init; }
    public string ShipCountry { get; init; } = string.Empty;
    public decimal SubTotal { get; init; }
    public decimal Total { get; init; }
    public List<OrderLineDto> Lines { get; init; } = new();
}

public sealed record OrderLineDto
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public short Quantity { get; init; }
    public float Discount { get; init; }
    public decimal LineTotal { get; init; }
}