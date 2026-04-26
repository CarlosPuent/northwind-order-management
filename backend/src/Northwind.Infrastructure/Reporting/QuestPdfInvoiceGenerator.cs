using Microsoft.Extensions.Options;
using Northwind.Application.Abstractions;
using Northwind.Domain.Common;
using Northwind.Domain.Entities;
using Northwind.Infrastructure.GoogleMaps;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Northwind.Infrastructure.Reporting;

/// <summary>
/// Generates branded PDF invoices for orders using QuestPDF.
/// Includes a header with company branding, order metadata, a line items table
/// with subtotals, freight, grand total, and optionally a Google Static Maps
/// thumbnail showing the delivery location.
/// </summary>
internal sealed class QuestPdfInvoiceGenerator : IInvoiceGenerator
{
    private readonly GoogleMapsOptions _mapsOptions;

    private static readonly Color BrandPrimary = Color.FromHex("#1F3A5F");
    private static readonly Color BrandAccent = Color.FromHex("#C8102E");
    private static readonly Color BrandSuccess = Color.FromHex("#2D7A3E");
    private static readonly Color CardBackground = Color.FromHex("#F4F6F9");
    private static readonly Color TableRowAlt = Color.FromHex("#F8FAFC");
    private static readonly Color BorderLight = Color.FromHex("#E2E8F0");
    private static readonly Color TextDark = Color.FromHex("#1A1A2E");
    private static readonly Color TextMuted = Color.FromHex("#64748B");
    private static readonly Color WarningBadge = Color.FromHex("#F59E0B");

    public QuestPdfInvoiceGenerator(IOptions<GoogleMapsOptions> mapsOptions)
    {
        _mapsOptions = mapsOptions.Value;
    }

    public Task<Result<byte[]>> GenerateAsync(
        Order order,
        string customerName,
        string employeeName,
        string? shipperName,
        ShippingGeocode? geocode,
        CancellationToken cancellationToken = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        try
        {
            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginHorizontal(40);
                    page.MarginVertical(35);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(TextDark));

                    page.Header().Element(c => ComposeHeader(c, order));
                    page.Content().Element(c => ComposeContent(
                        c, order, customerName, employeeName, shipperName, geocode));
                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf();

            return Task.FromResult(Result.Success<byte[]>(pdf));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<byte[]>(
                Error.Failure("Invoice.GenerationFailed", $"PDF generation failed: {ex.Message}")));
        }
    }

    // ------------------------------------------------------------------
    // Header
    // ------------------------------------------------------------------

    private static void ComposeHeader(IContainer container, Order order)
    {
        container.Column(outerCol =>
        {
            outerCol.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("NORTHWIND TRADERS")
                        .FontSize(22).Bold().FontColor(BrandPrimary).LetterSpacing(0.05f);
                    col.Item().PaddingTop(2).Text("Global Shipping & Distribution")
                        .FontSize(9).FontColor(TextMuted).Italic();
                });

                row.ConstantItem(200).Column(col =>
                {
                    col.Item().AlignRight().Text("INVOICE")
                        .FontSize(11).Bold().FontColor(TextMuted).LetterSpacing(0.15f);
                    col.Item().AlignRight().Text($"#{order.Id:D6}")
                        .FontSize(20).Bold().FontColor(BrandAccent);
                    col.Item().AlignRight().PaddingTop(4).Text($"{order.OrderDate:MMMM dd, yyyy}")
                        .FontSize(9).FontColor(TextMuted);
                });
            });

            outerCol.Item().PaddingTop(10).LineHorizontal(3).LineColor(BrandPrimary);
            outerCol.Item().PaddingTop(1).LineHorizontal(1).LineColor(BrandAccent);
        });
    }

    // ------------------------------------------------------------------
    // Content
    // ------------------------------------------------------------------

    private void ComposeContent(
        IContainer container,
        Order order,
        string customerName,
        string employeeName,
        string? shipperName,
        ShippingGeocode? geocode)
    {
        container.PaddingVertical(12).Column(col =>
        {
            // Status badge.
            col.Item().PaddingBottom(8).Row(row =>
            {
                if (order.IsShipped)
                {
                    row.AutoItem().Background(BrandSuccess).Padding(4).PaddingHorizontal(10)
                        .Text("SHIPPED").FontSize(8).Bold().FontColor(Colors.White);
                    row.AutoItem().PaddingLeft(8).AlignMiddle()
                        .Text($"Shipped on {order.ShippedDate:MMM dd, yyyy}")
                        .FontSize(8).FontColor(TextMuted);
                }
                else
                {
                    row.AutoItem().Background(WarningBadge).Padding(4).PaddingHorizontal(10)
                        .Text("PENDING").FontSize(8).Bold().FontColor(Colors.White);
                }
            });

            // Info cards.
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(c => ComposeInfoCard(c, "SHIP TO", new[]
                {
                    customerName,
                    order.ShipName,
                    order.ShipAddress.Street,
                    FormatCityLine(order.ShipAddress.City, order.ShipAddress.Region, order.ShipAddress.PostalCode),
                    order.ShipAddress.Country
                }));

                row.ConstantItem(15);

                row.RelativeItem().Element(c => ComposeInfoCard(c, "ORDER INFO", new[]
                {
                    $"Processed by: {employeeName}",
                    $"Carrier: {shipperName ?? "Not yet assigned"}",
                    $"Required by: {order.RequiredDate?.ToString("MMM dd, yyyy") ?? "No deadline"}",
                    $"Freight charge: ${order.Freight.Amount:N2}"
                }));
            });

            col.Item().PaddingVertical(12);

            col.Item().PaddingBottom(6).Text("LINE ITEMS")
                .FontSize(10).Bold().FontColor(BrandPrimary).LetterSpacing(0.1f);

            // Line items table.
            col.Item().Element(c => ComposeTable(c, order));

            col.Item().PaddingVertical(8);

            // Totals.
            col.Item().AlignRight().Width(260).Element(c => ComposeTotals(c, order));

            // Map thumbnail.
            if (geocode != null && !string.IsNullOrWhiteSpace(_mapsOptions.ApiKey))
            {
                col.Item().PaddingTop(20).Element(c => ComposeMapSection(c, geocode));
            }
        });
    }

    // ------------------------------------------------------------------
    // Info card
    // ------------------------------------------------------------------

    private static void ComposeInfoCard(IContainer container, string title, string[] lines)
    {
        container.Border(1).BorderColor(BorderLight).Background(CardBackground)
            .Padding(12).Column(col =>
            {
                col.Item().Text(title)
                    .FontSize(8).Bold().FontColor(BrandPrimary).LetterSpacing(0.1f);
                col.Item().PaddingTop(6);
                foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    col.Item().PaddingTop(1).Text(line).FontSize(9).FontColor(TextDark);
                }
            });
    }

    // ------------------------------------------------------------------
    // Line items table (all inline — avoids type compatibility issues)
    // ------------------------------------------------------------------

    private static void ComposeTable(IContainer container, Order order)
    {
        container.Border(1).BorderColor(BorderLight).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(40);
                cols.RelativeColumn(3);
                cols.ConstantColumn(80);
                cols.ConstantColumn(50);
                cols.ConstantColumn(65);
                cols.ConstantColumn(90);
            });

            // Header row — built inline to avoid API version mismatches.
            table.Header(header =>
            {
                header.Cell().Background(BrandPrimary).PaddingVertical(7).PaddingHorizontal(8)
                    .Text("#").FontColor(Colors.White).Bold().FontSize(8).AlignCenter();

                header.Cell().Background(BrandPrimary).PaddingVertical(7).PaddingHorizontal(8)
                    .Text("Product").FontColor(Colors.White).Bold().FontSize(8);

                header.Cell().Background(BrandPrimary).PaddingVertical(7).PaddingHorizontal(8)
                    .Text("Unit Price").FontColor(Colors.White).Bold().FontSize(8).AlignRight();

                header.Cell().Background(BrandPrimary).PaddingVertical(7).PaddingHorizontal(8)
                    .Text("Qty").FontColor(Colors.White).Bold().FontSize(8).AlignCenter();

                header.Cell().Background(BrandPrimary).PaddingVertical(7).PaddingHorizontal(8)
                    .Text("Discount").FontColor(Colors.White).Bold().FontSize(8).AlignCenter();

                header.Cell().Background(BrandPrimary).PaddingVertical(7).PaddingHorizontal(8)
                    .Text("Total").FontColor(Colors.White).Bold().FontSize(8).AlignRight();
            });

            // Data rows with alternating background.
            var lineNumber = 1;
            foreach (var line in order.Lines)
            {
                var rowBg = lineNumber % 2 == 0 ? TableRowAlt : Colors.White;

                table.Cell().Background(rowBg).BorderBottom(1).BorderColor(BorderLight)
                    .PaddingVertical(5).PaddingHorizontal(8)
                    .Text($"{lineNumber}").FontSize(9).AlignCenter();

                table.Cell().Background(rowBg).BorderBottom(1).BorderColor(BorderLight)
                    .PaddingVertical(5).PaddingHorizontal(8)
                    .Text($"Product #{line.ProductId}").FontSize(9);

                table.Cell().Background(rowBg).BorderBottom(1).BorderColor(BorderLight)
                    .PaddingVertical(5).PaddingHorizontal(8)
                    .Text($"${line.UnitPrice.Amount:N2}").FontSize(9).AlignRight();

                table.Cell().Background(rowBg).BorderBottom(1).BorderColor(BorderLight)
                    .PaddingVertical(5).PaddingHorizontal(8)
                    .Text($"{line.Quantity}").FontSize(9).AlignCenter();

                table.Cell().Background(rowBg).BorderBottom(1).BorderColor(BorderLight)
                    .PaddingVertical(5).PaddingHorizontal(8)
                    .Text(line.Discount > 0 ? $"{line.Discount:P0}" : "—").FontSize(9).AlignCenter();

                table.Cell().Background(rowBg).BorderBottom(1).BorderColor(BorderLight)
                    .PaddingVertical(5).PaddingHorizontal(8)
                    .Text($"${line.LineTotal.Amount:N2}").FontSize(9).AlignRight();

                lineNumber++;
            }
        });
    }

    // ------------------------------------------------------------------
    // Totals
    // ------------------------------------------------------------------

    private static void ComposeTotals(IContainer container, Order order)
    {
        container.Border(1).BorderColor(BorderLight).Background(CardBackground)
            .Padding(12).Column(col =>
            {
                col.Item().PaddingVertical(2).Row(r =>
                {
                    r.RelativeItem().Text("Subtotal").FontSize(9).AlignRight();
                    r.ConstantItem(10);
                    r.ConstantItem(90).Text($"${order.SubTotal.Amount:N2}").FontSize(9).AlignRight();
                });

                col.Item().PaddingVertical(2).Row(r =>
                {
                    r.RelativeItem().Text("Freight").FontSize(9).AlignRight();
                    r.ConstantItem(10);
                    r.ConstantItem(90).Text($"${order.Freight.Amount:N2}").FontSize(9).AlignRight();
                });

                col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(BorderLight);

                col.Item().PaddingTop(4).Row(r =>
                {
                    r.RelativeItem().Text("GRAND TOTAL").Bold().FontSize(12)
                        .FontColor(BrandPrimary).AlignRight();
                    r.ConstantItem(10);
                    r.ConstantItem(90).Text($"${order.Total.Amount:N2}").Bold().FontSize(12)
                        .FontColor(BrandPrimary).AlignRight();
                });
            });
    }

    // ------------------------------------------------------------------
    // Map thumbnail section
    // ------------------------------------------------------------------

    private void ComposeMapSection(IContainer container, ShippingGeocode geocode)
    {
        container.Border(1).BorderColor(BorderLight).Background(CardBackground)
            .Padding(12).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("DELIVERY LOCATION")
                        .FontSize(8).Bold().FontColor(BrandPrimary).LetterSpacing(0.1f);
                    col.Item().PaddingTop(6).Text(geocode.StandardizedAddress.ToSingleLine())
                        .FontSize(9).FontColor(TextDark);
                    col.Item().PaddingTop(2).Text($"Coordinates: {geocode.Coordinates}")
                        .FontSize(8).FontColor(TextMuted);
                    col.Item().PaddingTop(2).Text($"Place type: {geocode.PlaceType}")
                        .FontSize(8).FontColor(TextMuted);

                    if (geocode.IsAccessibleForHeavyFreight)
                    {
                        col.Item().PaddingTop(6)
                            .Background(BrandSuccess).Padding(4).PaddingHorizontal(8)
                            .Text("✓ ACCESSIBLE FOR HEAVY FREIGHT")
                            .FontSize(7).Bold().FontColor(Colors.White);
                    }
                });

                row.ConstantItem(15);

                row.ConstantItem(180).Height(135)
                    .Border(1).BorderColor(BorderLight)
                    .Image(GetStaticMapBytes(geocode));
            });
    }

    private byte[] GetStaticMapBytes(ShippingGeocode geocode)
    {
        var coords = geocode.Coordinates.ToMapsQuery();
        var url = $"{_mapsOptions.StaticMapsEndpoint}?" +
                  $"center={coords}&zoom=15&size=360x270&scale=2" +
                  $"&maptype=roadmap" +
                  $"&markers=color:red|{coords}" +
                  $"&key={_mapsOptions.ApiKey}";

        using var client = new HttpClient();
        try
        {
            var response = client.GetAsync(url).GetAwaiter().GetResult();
            return response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        }
        catch
        {
            return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        }
    }

    // ------------------------------------------------------------------
    // Footer
    // ------------------------------------------------------------------

    private static void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor(BorderLight);
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text("Northwind Traders · Order Management System")
                    .FontSize(7).FontColor(TextMuted);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(7).FontColor(TextMuted);
                    text.CurrentPageNumber().FontSize(7).FontColor(TextMuted);
                    text.Span(" of ").FontSize(7).FontColor(TextMuted);
                    text.TotalPages().FontSize(7).FontColor(TextMuted);
                });
            });
        });
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static string FormatCityLine(string city, string? region, string? postalCode)
    {
        var parts = new List<string> { city };
        if (!string.IsNullOrWhiteSpace(region)) parts.Add(region);
        if (!string.IsNullOrWhiteSpace(postalCode)) parts.Add(postalCode);
        return string.Join(", ", parts);
    }
}