using Northwind.Domain.Common;
using Northwind.Domain.Entities;

namespace Northwind.Application.Abstractions;

/// <summary>
/// Generates a styled PDF invoice for a given Order.
/// The implementation lives in Infrastructure (QuestPDF); Application
/// only depends on this interface.
/// </summary>
public interface IInvoiceGenerator
{
    /// <summary>
    /// Generates a PDF byte array for the given order.
    /// </summary>
    /// <param name="order">The order with lines loaded.</param>
    /// <param name="customerName">The customer's company name for display.</param>
    /// <param name="employeeName">The employee's full name for display.</param>
    /// <param name="shipperName">The shipper's company name, or null if not assigned.</param>
    /// <param name="geocode">Optional geocode for the map thumbnail.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<byte[]>> GenerateAsync(
        Order order,
        string customerName,
        string employeeName,
        string? shipperName,
        ShippingGeocode? geocode,
        CancellationToken cancellationToken = default);
}