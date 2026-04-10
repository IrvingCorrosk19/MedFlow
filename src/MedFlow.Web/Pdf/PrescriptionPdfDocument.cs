using MedFlow.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MedFlow.Web.Pdf;

public sealed class PrescriptionPdfDocument : IDocument
{
    private readonly string _clinicName;
    private readonly string? _clinicAddress;
    private readonly string? _clinicPhone;
    private readonly string _patientName;
    private readonly string? _patientDocument;
    private readonly DateTime? _patientDob;
    private readonly IReadOnlyList<Prescription> _prescriptions;
    private readonly DateTime _issuedAt;
    private readonly int _printNumber;

    private static readonly string HeaderColor = "#1a6b3c";

    public PrescriptionPdfDocument(
        string clinicName, string? clinicAddress, string? clinicPhone,
        string patientName, string? patientDocument, DateTime? patientDob,
        IReadOnlyList<Prescription> prescriptions,
        DateTime issuedAt, int printNumber)
    {
        _clinicName = clinicName;
        _clinicAddress = clinicAddress;
        _clinicPhone = clinicPhone;
        _patientName = patientName;
        _patientDocument = patientDocument;
        _patientDob = patientDob;
        _prescriptions = prescriptions;
        _issuedAt = issuedAt;
        _printNumber = printNumber;
    }

    public DocumentMetadata GetMetadata() => new DocumentMetadata
    {
        Title = $"Receta médica — {_patientName}",
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
            page.Footer().Row(row =>
            {
                row.RelativeItem().Text($"Válida por 30 días desde la fecha de emisión.").FontSize(8).FontColor(Colors.Grey.Medium);
                row.ConstantItem(120).AlignRight().Text($"Copia #{_printNumber}  |  Pág. ").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(_clinicName).FontSize(18).Bold().FontColor(HeaderColor);
                    if (!string.IsNullOrWhiteSpace(_clinicAddress))
                        c.Item().Text(_clinicAddress).FontSize(9).FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrWhiteSpace(_clinicPhone))
                        c.Item().Text($"Tel: {_clinicPhone}").FontSize(9).FontColor(Colors.Grey.Darken2);
                });
                row.ConstantItem(160).Column(c =>
                {
                    c.Item().Background(HeaderColor).Padding(8).Column(inner =>
                    {
                        inner.Item().Text("RECETA MÉDICA").FontSize(13).Bold().FontColor(Colors.White).AlignCenter();
                        inner.Item().Text(_issuedAt.ToString("dd/MM/yyyy")).FontSize(10).FontColor(Colors.Grey.Lighten3).AlignCenter();
                    });
                });
            });
            col.Item().PaddingTop(6).BorderBottom(2).BorderColor(HeaderColor);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            // Datos del paciente
            col.Item().PaddingTop(12).Background("#f4f9f6").Padding(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("PACIENTE").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                    c.Item().Text(_patientName).FontSize(13).Bold().FontColor(HeaderColor);
                    if (!string.IsNullOrWhiteSpace(_patientDocument))
                        c.Item().Text($"Doc: {_patientDocument}").FontSize(9).FontColor(Colors.Grey.Darken2);
                });
                if (_patientDob.HasValue)
                    row.ConstantItem(130).Column(c =>
                    {
                        c.Item().Text("FECHA DE NACIMIENTO").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                        c.Item().Text(_patientDob.Value.ToString("dd/MM/yyyy")).FontSize(10);
                        var age = (int)((DateTime.Today - _patientDob.Value).TotalDays / 365.25);
                        c.Item().Text($"{age} años").FontSize(9).FontColor(Colors.Grey.Darken2);
                    });
            });

            // Medicamentos
            col.Item().PaddingTop(16).Text("Rp/").FontSize(14).Bold().FontColor(HeaderColor).Italic();
            col.Item().PaddingTop(4);

            var order = 1;
            foreach (var rx in _prescriptions)
            {
                col.Item().PaddingTop(8).BorderLeft(3).BorderColor(HeaderColor).PaddingLeft(10).Column(med =>
                {
                    med.Item().Row(r =>
                    {
                        r.ConstantItem(20).Text($"{order++}.").Bold().FontSize(10);
                        r.RelativeItem().Text(rx.MedicationName).Bold().FontSize(11);
                    });
                    if (!string.IsNullOrWhiteSpace(rx.Dosage))
                        med.Item().PaddingLeft(20).Text($"Dosis: {rx.Dosage}").FontSize(10).FontColor(Colors.Grey.Darken3);
                    if (!string.IsNullOrWhiteSpace(rx.Frequency))
                        med.Item().PaddingLeft(20).Text($"Frecuencia: {rx.Frequency}").FontSize(10).FontColor(Colors.Grey.Darken3);
                    if (!string.IsNullOrWhiteSpace(rx.Duration))
                        med.Item().PaddingLeft(20).Text($"Duración: {rx.Duration}").FontSize(10).FontColor(Colors.Grey.Darken3);
                    if (!string.IsNullOrWhiteSpace(rx.Instructions))
                        med.Item().PaddingLeft(20).Text($"Indicaciones: {rx.Instructions}").FontSize(9)
                            .FontColor(Colors.Grey.Medium).Italic();
                });
            }

            // Firma del médico
            var prescriber = _prescriptions.FirstOrDefault();
            if (prescriber != null && !string.IsNullOrWhiteSpace(prescriber.PrescriberName))
            {
                col.Item().PaddingTop(32).AlignRight().Width(200).Column(sign =>
                {
                    sign.Item().BorderBottom(1).BorderColor(Colors.Grey.Medium).Height(30);
                    sign.Item().PaddingTop(4).Text(prescriber.PrescriberName).FontSize(10).Bold().AlignCenter();
                    if (!string.IsNullOrWhiteSpace(prescriber.PrescriberLicense))
                        sign.Item().Text($"Ced. {prescriber.PrescriberLicense}").FontSize(9).FontColor(Colors.Grey.Medium).AlignCenter();
                });
            }
        });
    }
}
