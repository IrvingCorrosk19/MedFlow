using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MedFlow.Web.Pdf;

public sealed record InvoiceLineItem(string Description, decimal Quantity, decimal UnitPrice, decimal Subtotal);

public sealed class InvoicePdfDocument : IDocument
{
    private readonly string _clinicName;
    private readonly string? _clinicAddress;
    private readonly string? _clinicPhone;
    private readonly string? _clinicTaxId;
    private readonly string _invoiceNumber;
    private readonly DateTime _invoiceDate;
    private readonly string _patientName;
    private readonly string? _patientDocument;
    private readonly IReadOnlyList<InvoiceLineItem> _items;
    private readonly decimal _subtotal;
    private readonly decimal _taxAmount;
    private readonly decimal _discount;
    private readonly decimal _total;
    private readonly string _status;

    private static readonly string HeaderColor = "#1a3c5e";
    private static readonly string AccentColor = "#2980b9";

    public InvoicePdfDocument(
        string clinicName, string? clinicAddress, string? clinicPhone, string? clinicTaxId,
        string invoiceNumber, DateTime invoiceDate,
        string patientName, string? patientDocument,
        IReadOnlyList<InvoiceLineItem> items,
        decimal subtotal, decimal taxAmount, decimal discount, decimal total,
        string status)
    {
        _clinicName = clinicName;
        _clinicAddress = clinicAddress;
        _clinicPhone = clinicPhone;
        _clinicTaxId = clinicTaxId;
        _invoiceNumber = invoiceNumber;
        _invoiceDate = invoiceDate;
        _patientName = patientName;
        _patientDocument = patientDocument;
        _items = items;
        _subtotal = subtotal;
        _taxAmount = taxAmount;
        _discount = discount;
        _total = total;
        _status = status;
    }

    public DocumentMetadata GetMetadata() => new DocumentMetadata
    {
        Title = $"Factura {_invoiceNumber}",
        Author = _clinicName,
        CreationDate = DateTimeOffset.UtcNow
    };

    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

            page.Header().Element(ComposeHeader);
            page.Content().PaddingVertical(10).Element(ComposeContent);
            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Página ").FontSize(8).FontColor(Colors.Grey.Medium);
                x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                x.Span(" de ").FontSize(8).FontColor(Colors.Grey.Medium);
                x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                x.Span($"  |  Generado el {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            // Izquierda: datos de la clínica
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(_clinicName).FontSize(18).Bold().FontColor(HeaderColor);
                if (!string.IsNullOrWhiteSpace(_clinicAddress))
                    col.Item().Text(_clinicAddress).FontSize(9).FontColor(Colors.Grey.Darken2);
                if (!string.IsNullOrWhiteSpace(_clinicPhone))
                    col.Item().Text($"Tel: {_clinicPhone}").FontSize(9).FontColor(Colors.Grey.Darken2);
                if (!string.IsNullOrWhiteSpace(_clinicTaxId))
                    col.Item().Text($"RUC/NIT: {_clinicTaxId}").FontSize(9).FontColor(Colors.Grey.Darken2);
            });

            // Derecha: número de factura
            row.ConstantItem(180).Column(col =>
            {
                col.Item().Background(HeaderColor).Padding(10).Column(inner =>
                {
                    inner.Item().Text("FACTURA").FontSize(16).Bold().FontColor(Colors.White).AlignRight();
                    inner.Item().Text($"# {_invoiceNumber}").FontSize(12).Bold().FontColor(Colors.White).AlignRight();
                    inner.Item().Text(_invoiceDate.ToString("dd/MM/yyyy")).FontSize(10).FontColor(Colors.Grey.Lighten3).AlignRight();
                    inner.Item().PaddingTop(4).Text(_status.ToUpperInvariant()).FontSize(9).Bold()
                        .FontColor(_status.ToLower() == "paid" || _status.ToLower() == "pagada" ? "#2ecc71" : "#f39c12")
                        .AlignRight();
                });
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            // Datos del paciente
            col.Item().PaddingTop(16).Background("#f4f6f9").Padding(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("PACIENTE").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                    c.Item().Text(_patientName).FontSize(12).Bold().FontColor(HeaderColor);
                    if (!string.IsNullOrWhiteSpace(_patientDocument))
                        c.Item().Text($"Doc: {_patientDocument}").FontSize(9).FontColor(Colors.Grey.Darken2);
                });
                row.ConstantItem(120).Column(c =>
                {
                    c.Item().Text("FECHA EMISIÓN").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                    c.Item().Text(_invoiceDate.ToString("dd/MM/yyyy")).FontSize(10);
                });
            });

            // Tabla de items
            col.Item().PaddingTop(16).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(4);  // Descripción
                    cols.RelativeColumn(1);  // Cant.
                    cols.RelativeColumn(2);  // Precio unit.
                    cols.RelativeColumn(2);  // Subtotal
                });

                // Header
                static IContainer HeaderCell(IContainer c) =>
                    c.Background(HeaderColor).Padding(6).AlignMiddle();

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Descripción").FontColor(Colors.White).Bold().FontSize(9);
                    header.Cell().Element(HeaderCell).AlignRight().Text("Cant.").FontColor(Colors.White).Bold().FontSize(9);
                    header.Cell().Element(HeaderCell).AlignRight().Text("Precio Unit.").FontColor(Colors.White).Bold().FontSize(9);
                    header.Cell().Element(HeaderCell).AlignRight().Text("Subtotal").FontColor(Colors.White).Bold().FontSize(9);
                });

                var row = 0;
                foreach (var item in _items)
                {
                    var bg = row++ % 2 == 0 ? "#FFFFFF" : "#f8f9fa";

                    static IContainer DataCell(IContainer c, string bg) =>
                        c.Background(bg).BorderBottom(1).BorderColor("#dee2e6").Padding(6).AlignMiddle();

                    table.Cell().Element(c => DataCell(c, bg)).Text(item.Description).FontSize(9);
                    table.Cell().Element(c => DataCell(c, bg)).AlignRight().Text(item.Quantity.ToString("N2")).FontSize(9);
                    table.Cell().Element(c => DataCell(c, bg)).AlignRight().Text(item.UnitPrice.ToString("N2")).FontSize(9);
                    table.Cell().Element(c => DataCell(c, bg)).AlignRight().Text(item.Subtotal.ToString("N2")).FontSize(9);
                }
            });

            // Totales
            col.Item().PaddingTop(8).AlignRight().Width(220).Column(totals =>
            {
                totals.Item().Row(r =>
                {
                    r.RelativeItem().Text("Subtotal:").FontSize(10);
                    r.ConstantItem(90).AlignRight().Text(_subtotal.ToString("N2")).FontSize(10);
                });
                if (_discount > 0)
                    totals.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Descuento:").FontSize(10).FontColor("#e74c3c");
                        r.ConstantItem(90).AlignRight().Text($"-{_discount:N2}").FontSize(10).FontColor("#e74c3c");
                    });
                if (_taxAmount > 0)
                    totals.Item().Row(r =>
                    {
                        r.RelativeItem().Text("IVA / Impuesto:").FontSize(10);
                        r.ConstantItem(90).AlignRight().Text(_taxAmount.ToString("N2")).FontSize(10);
                    });
                totals.Item().Background(HeaderColor).Padding(6).Row(r =>
                {
                    r.RelativeItem().Text("TOTAL").FontSize(12).Bold().FontColor(Colors.White);
                    r.ConstantItem(100).AlignRight().Text(_total.ToString("N2")).FontSize(12).Bold().FontColor(Colors.White);
                });
            });

            // Pie
            col.Item().PaddingTop(24).BorderTop(1).BorderColor("#dee2e6").PaddingTop(8)
                .Text("Gracias por confiar en nosotros. Este documento es válido como comprobante de pago.")
                .FontSize(8).FontColor(Colors.Grey.Medium).AlignCenter();
        });
    }
}
