using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniNaturalFarms.Application.Common;
using MiniNaturalFarms.Application.Orders;
using MiniNaturalFarms.Domain.Entities;
using MiniNaturalFarms.Domain.Enums;
using MiniNaturalFarms.Infrastructure.Persistence;
using MiniNaturalFarms.Infrastructure.Settings;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MiniNaturalFarms.Infrastructure.Orders;

public class InvoiceService : IInvoiceService
{
    private static readonly CultureInfo Inr = CultureInfo.GetCultureInfo("en-IN");

    private readonly AppDbContext _db;
    private readonly BusinessSettings _business;

    static InvoiceService()
    {
        // Free for individuals and companies under US$1M annual revenue
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public InvoiceService(AppDbContext db, IOptions<BusinessSettings> business)
    {
        _db = db;
        _business = business.Value;
    }

    public async Task<InvoiceFile> GenerateAsync(int orderId, string? userId, CancellationToken ct = default)
    {
        var order = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && (userId == null || o.UserId == userId), ct)
            ?? throw new NotFoundException("Order not found.");

        var invoiceable = order.Status != OrderStatus.Cancelled
                          && (order.PaymentMethod == PaymentMethod.Cod || order.PaymentStatus == PaymentStatus.Paid);
        if (!invoiceable)
            throw new ConflictException("The invoice is available once the order is confirmed.");

        return new InvoiceFile(Render(order), $"Invoice-{order.OrderNumber}.pdf");
    }

    private byte[] Render(Order o)
    {
        var b = _business;
        var date = o.CreatedAt.AddHours(5.5).ToString("dd MMM yyyy", CultureInfo.InvariantCulture); // IST
        string Money(decimal v) => "Rs. " + v.ToString("N2", Inr);

        var paymentText = o.PaymentMethod == PaymentMethod.Cod
            ? "Cash on Delivery"
            : "Paid online" + (string.IsNullOrEmpty(o.RazorpayPaymentId) ? "" : $" (Ref: {o.RazorpayPaymentId})");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(b.Name).FontSize(18).Bold();
                        if (!string.IsNullOrWhiteSpace(b.Address)) col.Item().Text(b.Address);
                        if (!string.IsNullOrWhiteSpace(b.Phone)) col.Item().Text($"Phone: {b.Phone}");
                        if (!string.IsNullOrWhiteSpace(b.Email)) col.Item().Text($"Email: {b.Email}");
                        if (!string.IsNullOrWhiteSpace(b.Gstin)) col.Item().Text($"GSTIN: {b.Gstin}");
                    });

                    row.ConstantItem(190).Column(col =>
                    {
                        col.Item().AlignRight().Text("INVOICE").FontSize(18).Bold();
                        col.Item().AlignRight().Text($"Invoice no: INV-{o.OrderNumber}");
                        col.Item().AlignRight().Text($"Order no: {o.OrderNumber}");
                        col.Item().AlignRight().Text($"Date: {date}");
                    });
                });

                page.Content().PaddingVertical(16).Column(col =>
                {
                    col.Spacing(14);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Ship to").Bold();
                            c.Item().Text(o.ShipFullName);
                            c.Item().Text(o.ShipAddress);
                            c.Item().Text($"{o.ShipCity}, {o.ShipState} - {o.ShipPincode}");
                            c.Item().Text($"Phone: {o.ShipPhone}");
                        });

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Payment").Bold();
                            c.Item().Text(paymentText);
                            c.Item().Text($"Status: {o.PaymentStatus}");
                        });
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(28);
                            c.RelativeColumn(5);
                            c.ConstantColumn(40);
                            c.ConstantColumn(80);
                            c.ConstantColumn(85);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(HeadCell).Text("#");
                            h.Cell().Element(HeadCell).Text("Item");
                            h.Cell().Element(HeadCell).AlignRight().Text("Qty");
                            h.Cell().Element(HeadCell).AlignRight().Text("Price");
                            h.Cell().Element(HeadCell).AlignRight().Text("Amount");
                        });

                        var n = 1;
                        foreach (var i in o.Items.OrderBy(x => x.Id))
                        {
                            table.Cell().Element(BodyCell).Text((n++).ToString());
                            table.Cell().Element(BodyCell).Text(i.ProductName);
                            table.Cell().Element(BodyCell).AlignRight().Text(i.Quantity.ToString());
                            table.Cell().Element(BodyCell).AlignRight().Text(Money(i.UnitPrice));
                            table.Cell().Element(BodyCell).AlignRight().Text(Money(i.UnitPrice * i.Quantity));
                        }
                    });

                    col.Item().AlignRight().Width(230).Column(t =>
                    {
                        t.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Subtotal");
                            r.ConstantItem(100).AlignRight().Text(Money(o.SubTotal));
                        });
                        t.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Shipping");
                            r.ConstantItem(100).AlignRight().Text(o.ShippingCharge == 0 ? "FREE" : Money(o.ShippingCharge));
                        });
                        t.Item().PaddingTop(4).BorderTop(1).Row(r =>
                        {
                            r.RelativeItem().Text("Total").Bold();
                            r.ConstantItem(100).AlignRight().Text(Money(o.TotalAmount)).Bold();
                        });
                        if (o.PaymentMethod == PaymentMethod.Cod)
                            t.Item().PaddingTop(4).Text("Amount payable on delivery").Italic();
                    });
                });

                page.Footer().Column(f =>
                {
                    f.Item().AlignCenter().Text($"Thank you for shopping with {b.Name}.").FontSize(9);
                    f.Item().AlignCenter().Text("This is a computer-generated invoice. Prices are inclusive of applicable taxes.")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();
    }

    private static IContainer HeadCell(IContainer c) =>
        c.Background(Colors.Grey.Lighten3).Padding(5).DefaultTextStyle(x => x.Bold());

    private static IContainer BodyCell(IContainer c) =>
        c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
}