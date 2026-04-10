using MedFlow.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MedFlow.Web.Pdf;

public sealed class MedicalRecordPdfDocument : IDocument
{
    private readonly string _clinicName;
    private readonly string? _clinicAddress;
    private readonly string? _clinicPhone;
    private readonly MedicalRecord _record;
    private readonly string _patientName;
    private readonly string? _patientDocument;
    private readonly string _doctorName;

    private static readonly string HeaderColor = "#1a3c5e";
    private static readonly string AccentColor = "#2980b9";

    public MedicalRecordPdfDocument(
        string clinicName, string? clinicAddress, string? clinicPhone,
        MedicalRecord record)
    {
        _clinicName = clinicName;
        _clinicAddress = clinicAddress;
        _clinicPhone = clinicPhone;
        _record = record;
        _patientName = record.Patient?.NombreCompleto ?? "—";
        _patientDocument = record.Patient?.NumeroDocumento;
        _doctorName = record.Doctor?.FullName ?? "—";
    }

    public DocumentMetadata GetMetadata() => new DocumentMetadata
    {
        Title = $"Historia clínica — {_patientName} — {_record.VisitDate:dd/MM/yyyy}",
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
            page.Content().PaddingVertical(8).Element(ComposeContent);
            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Página ").FontSize(8).FontColor(Colors.Grey.Medium);
                x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                x.Span(" de ").FontSize(8).FontColor(Colors.Grey.Medium);
                x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                x.Span($"  |  Documento confidencial — {_clinicName}  |  Generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(8).FontColor(Colors.Grey.Medium);
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
                row.ConstantItem(180).Column(c =>
                {
                    c.Item().Background(HeaderColor).Padding(10).Column(inner =>
                    {
                        inner.Item().Text("HISTORIA CLÍNICA").FontSize(13).Bold().FontColor(Colors.White).AlignRight();
                        inner.Item().Text(_record.VisitDate.ToString("dd/MM/yyyy HH:mm")).FontSize(10)
                            .FontColor(Colors.Grey.Lighten3).AlignRight();
                        inner.Item().PaddingTop(4).Text($"Dr(a). {_doctorName}").FontSize(9)
                            .FontColor(Colors.Grey.Lighten3).AlignRight();
                    });
                });
            });
            col.Item().PaddingTop(4).BorderBottom(2).BorderColor(HeaderColor);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            // Datos del paciente
            col.Item().PaddingTop(10).Background("#f4f6f9").Padding(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("PACIENTE").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                    c.Item().Text(_patientName).FontSize(13).Bold().FontColor(HeaderColor);
                    if (!string.IsNullOrWhiteSpace(_patientDocument))
                        c.Item().Text($"Doc: {_patientDocument}").FontSize(9).FontColor(Colors.Grey.Darken2);
                });
                row.ConstantItem(160).Column(c =>
                {
                    c.Item().Text("FECHA DE CONSULTA").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                    c.Item().Text(_record.VisitDate.ToString("dd/MM/yyyy")).FontSize(11).Bold();
                    c.Item().Text(_record.VisitDate.ToString("HH:mm") + " hrs").FontSize(9).FontColor(Colors.Grey.Darken2);
                });
            });

            // Signos vitales
            var hasVitals = _record.HeightCm.HasValue || _record.WeightKg.HasValue
                || !string.IsNullOrWhiteSpace(_record.BloodPressure)
                || _record.HeartRateBpm.HasValue || _record.TemperatureCelsius.HasValue;

            if (hasVitals)
            {
                col.Item().PaddingTop(10).Text("Signos vitales").FontSize(11).Bold().FontColor(HeaderColor);
                col.Item().PaddingTop(4).Row(row =>
                {
                    if (_record.BloodPressure != null)
                        AddVitalBox(row, "PA", _record.BloodPressure, "mmHg");
                    if (_record.HeartRateBpm.HasValue)
                        AddVitalBox(row, "FC", _record.HeartRateBpm.Value.ToString(), "lpm");
                    if (_record.TemperatureCelsius.HasValue)
                        AddVitalBox(row, "T°", _record.TemperatureCelsius.Value.ToString("F1"), "°C");
                    if (_record.WeightKg.HasValue)
                        AddVitalBox(row, "Peso", _record.WeightKg.Value.ToString("F1"), "kg");
                    if (_record.HeightCm.HasValue)
                        AddVitalBox(row, "Talla", _record.HeightCm.Value.ToString("F0"), "cm");
                    row.RelativeItem();
                });
            }

            // Motivo de consulta
            AddSection(col, "Motivo de consulta", _record.ChiefComplaint, AccentColor);

            // Diagnóstico
            AddSection(col, "Diagnóstico", _record.Diagnosis, "#8e44ad");

            // Plan de tratamiento
            AddSection(col, "Plan de tratamiento", _record.TreatmentPlan, "#27ae60");

            // Notas clínicas
            AddSection(col, "Notas clínicas", _record.ClinicalNotes, HeaderColor);

            // Observaciones
            if (!string.IsNullOrWhiteSpace(_record.Observations))
                AddSection(col, "Observaciones", _record.Observations, "#7f8c8d");

            // Receta médica
            if (_record.Prescriptions.Any())
            {
                col.Item().PaddingTop(14).Text("Receta médica").FontSize(11).Bold().FontColor("#1a6b3c");
                col.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(3);
                    });

                    static IContainer Hdr(IContainer c) =>
                        c.Background("#1a6b3c").Padding(5).AlignMiddle();

                    table.Header(h =>
                    {
                        h.Cell().Element(Hdr).Text("Medicamento").FontColor(Colors.White).Bold().FontSize(9);
                        h.Cell().Element(Hdr).Text("Dosis").FontColor(Colors.White).Bold().FontSize(9);
                        h.Cell().Element(Hdr).Text("Frecuencia").FontColor(Colors.White).Bold().FontSize(9);
                        h.Cell().Element(Hdr).Text("Duración").FontColor(Colors.White).Bold().FontSize(9);
                        h.Cell().Element(Hdr).Text("Indicaciones").FontColor(Colors.White).Bold().FontSize(9);
                    });

                    var i = 0;
                    foreach (var rx in _record.Prescriptions)
                    {
                        var bg = i++ % 2 == 0 ? "#FFFFFF" : "#f8f9fa";
                        static IContainer Cell(IContainer c, string bg) =>
                            c.Background(bg).BorderBottom(1).BorderColor("#dee2e6").Padding(5).AlignMiddle();

                        table.Cell().Element(c => Cell(c, bg)).Text(rx.MedicationName).FontSize(9).Bold();
                        table.Cell().Element(c => Cell(c, bg)).Text(rx.Dosage ?? "—").FontSize(9);
                        table.Cell().Element(c => Cell(c, bg)).Text(rx.Frequency ?? "—").FontSize(9);
                        table.Cell().Element(c => Cell(c, bg)).Text(rx.Duration ?? "—").FontSize(9);
                        table.Cell().Element(c => Cell(c, bg)).Text(rx.Instructions ?? "—").FontSize(9);
                    }
                });
            }

            // Firma
            col.Item().PaddingTop(32).AlignRight().Width(200).Column(sign =>
            {
                sign.Item().BorderBottom(1).BorderColor(Colors.Grey.Medium).Height(30);
                sign.Item().PaddingTop(4).Text(_doctorName).FontSize(10).Bold().AlignCenter();
                sign.Item().Text("Médico tratante").FontSize(8).FontColor(Colors.Grey.Medium).AlignCenter();
            });

            // Pie confidencialidad
            col.Item().PaddingTop(16).BorderTop(1).BorderColor("#dee2e6").PaddingTop(6)
                .Text("DOCUMENTO CONFIDENCIAL — Esta historia clínica contiene información protegida por ley. Su divulgación no autorizada está prohibida.")
                .FontSize(7).FontColor(Colors.Grey.Medium).AlignCenter().Italic();
        });
    }

    private static void AddVitalBox(RowDescriptor row, string label, string value, string unit)
    {
        row.AutoItem().Border(1).BorderColor("#dee2e6").Padding(6).MinWidth(60).Column(c =>
        {
            c.Item().Text(label).FontSize(7).Bold().FontColor(Colors.Grey.Medium).AlignCenter();
            c.Item().Text(value).FontSize(13).Bold().AlignCenter();
            c.Item().Text(unit).FontSize(7).FontColor(Colors.Grey.Medium).AlignCenter();
        });
    }

    private static void AddSection(ColumnDescriptor col, string title, string? content, string color)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        col.Item().PaddingTop(12).Text(title).FontSize(11).Bold().FontColor(color);
        col.Item().PaddingTop(4).BorderLeft(3).BorderColor(color).PaddingLeft(8)
            .Text(content).FontSize(10).LineHeight(1.4f);
    }
}
