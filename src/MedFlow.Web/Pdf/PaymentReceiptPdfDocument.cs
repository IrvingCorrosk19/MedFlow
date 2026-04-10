using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MedFlow.Web.Pdf;

public sealed class PaymentReceiptPdfDocument : IDocument
{
    private readonly string _clinicName;
    private readonly string? _clinicAddress;
    private readonly string? _clinicPhone;
    private readonly string _patientName;
    private readonly string _invoiceNumber;
    private readonly string _receiptNumber;
    private readonly DateTime _paymentDate;
    private readonly decimal _amount;
    private readonly string _paymentMethod;
    private readonly string? _referenceNumber;
    private readonly string? _notes;

    private static readonly string HeaderColor = "#1a3c5e";
    private static readonly string AccentColor = "#27ae60";

    public PaymentReceiptPdfDocument(
        string clinicName,
        string? clinicAddress,
        string? clinicPhone,
        string patientName,
        string invoiceNumber,
        string receiptNumber,
        DateTime paymentDate,
        decimal amount,
        string paymentMethod,
        string? referenceNumber,
        string? notes)
    {
        _clinicName = clinicName;
        _clinicAddress = clinicAddress;
        _clinicPhone = clinicPhone;
        _patientName = patientName;
        _invoiceNumber = invoiceNumber;
        _receiptNumber = receiptNumber;
        _paymentDate = paymentDate;
        _amount = amount;
        _paymentMethod = paymentMethod;
        _referenceNumber = referenceNumber;
        _notes = notes;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A5.Landscape());
            page.Margin(30);
            page.DefaultTextStyle(t => t.FontFamily("Helvetica").FontSize(10));

            page.Content().Column(col =>
            {
                // Header
                col.Item().Row(row =>
                {
                    row.RelativeItem().Background(HeaderColor).Padding(12).Column(inner =>
                    {
                        inner.Item().Text(_clinicName)
                            .FontFamily("Helvetica").Bold().FontSize(16).FontColor("#FFFFFF");
                        if (!string.IsNullOrWhiteSpace(_clinicAddress))
                            inner.Item().Text(_clinicAddress).FontColor("#FFFFFF").FontSize(8);
                        if (!string.IsNullOrWhiteSpace(_clinicPhone))
                            inner.Item().Text(_clinicPhone).FontColor("#FFFFFF").FontSize(8);
                    });
                    row.ConstantItem(130).Background(AccentColor).Padding(12).Column(inner =>
                    {
                        inner.Item().Text("RECIBO DE PAGO").Bold().FontSize(14).FontColor("#FFFFFF").AlignCenter();
                        inner.Item().Text($"N° {_receiptNumber}").FontColor("#FFFFFF").FontSize(9).AlignCenter();
                        inner.Item().Text(_paymentDate.ToLocalTime().ToString("dd/MM/yyyy")).FontColor("#FFFFFF").FontSize(9).AlignCenter();
                    });
                });

                col.Item().PaddingTop(16).Row(row =>
                {
                    // Patient info
                    row.RelativeItem().Border(1).BorderColor("#dee2e6").Padding(10).Column(inner =>
                    {
                        inner.Item().Text("PACIENTE").Bold().FontSize(8).FontColor("#6c757d");
                        inner.Item().PaddingTop(2).Text(_patientName).Bold().FontSize(12);
                    });

                    row.ConstantItem(10);

                    // Invoice ref
                    row.RelativeItem().Border(1).BorderColor("#dee2e6").Padding(10).Column(inner =>
                    {
                        inner.Item().Text("FACTURA APLICADA").Bold().FontSize(8).FontColor("#6c757d");
                        inner.Item().PaddingTop(2).Text(_invoiceNumber).FontSize(12);
                    });
                });

                // Amount + method
                col.Item().PaddingTop(12).Row(row =>
                {
                    row.RelativeItem(2).Background("#f8fff8").Border(1).BorderColor(AccentColor).Padding(12).Column(inner =>
                    {
                        inner.Item().Text("MONTO RECIBIDO").Bold().FontSize(9).FontColor(AccentColor);
                        inner.Item().Text($"{_amount:N2}").Bold().FontSize(28).FontColor(HeaderColor);
                    });

                    row.ConstantItem(10);

                    row.RelativeItem().Border(1).BorderColor("#dee2e6").Padding(12).Column(inner =>
                    {
                        inner.Item().Text("MÉTODO DE PAGO").Bold().FontSize(8).FontColor("#6c757d");
                        inner.Item().PaddingTop(4).Text(_paymentMethod).FontSize(11);
                        if (!string.IsNullOrWhiteSpace(_referenceNumber))
                        {
                            inner.Item().PaddingTop(4).Text("REF:").Bold().FontSize(8).FontColor("#6c757d");
                            inner.Item().Text(_referenceNumber).FontSize(10);
                        }
                    });
                });

                if (!string.IsNullOrWhiteSpace(_notes))
                {
                    col.Item().PaddingTop(8).Text($"Notas: {_notes}").FontSize(8).FontColor("#6c757d").Italic();
                }

                // Footer
                col.Item().PaddingTop(16).LineHorizontal(0.5f).LineColor("#dee2e6");
                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text($"Generado: {DateTime.UtcNow.ToLocalTime():dd/MM/yyyy HH:mm}")
                        .FontSize(7).FontColor("#adb5bd");
                    row.RelativeItem().AlignRight().Text("Documento no fiscal — MedFlow")
                        .FontSize(7).FontColor("#adb5bd");
                });
            });
        });
    }
}
