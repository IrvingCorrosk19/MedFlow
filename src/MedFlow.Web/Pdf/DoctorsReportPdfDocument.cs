using MedFlow.Application.Reporting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MedFlow.Web.Pdf;

public sealed class DoctorsReportPdfDocument : IDocument
{
    private readonly string _clinicName;
    private readonly DoctorsReportVm _vm;
    private readonly DateTime? _from;
    private readonly DateTime? _to;

    private static readonly string HeaderColor = "#1a3c5e";
    private static readonly string AccentColor = "#8e44ad";

    public DoctorsReportPdfDocument(string clinicName, DoctorsReportVm vm, DateTime? from, DateTime? to)
    {
        _clinicName = clinicName;
        _vm = vm;
        _from = from;
        _to = to;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(t => t.FontFamily("Helvetica").FontSize(9));

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text(_clinicName).Bold().FontSize(16).FontColor(HeaderColor);
                        inner.Item().Text("Reporte de Productividad de Doctores").FontSize(10).FontColor("#6c757d");
                    });
                    row.ConstantItem(120).AlignRight().Column(inner =>
                    {
                        var range = (_from.HasValue || _to.HasValue)
                            ? $"{_from?.ToString("dd/MM/yyyy") ?? "—"} – {_to?.ToString("dd/MM/yyyy") ?? "—"}"
                            : "Todos los períodos";
                        inner.Item().Text(range).FontSize(8).FontColor("#6c757d").AlignRight();
                        inner.Item().Text($"Generado: {DateTime.UtcNow.ToLocalTime():dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#adb5bd").AlignRight();
                    });
                });
                col.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(HeaderColor);
            });

            page.Content().PaddingTop(12).Column(col =>
            {
                // Summary cards
                col.Item().Row(row =>
                {
                    SummaryCard(row, "Total doctores", _vm.Rows.Count.ToString(), AccentColor);
                    SummaryCard(row, "Total citas", _vm.Rows.Sum(r => r.TotalAppointments).ToString(), "#2980b9");
                    SummaryCard(row, "Completadas", _vm.Rows.Sum(r => r.Completed).ToString(), "#27ae60");
                    SummaryCard(row, "Promedio citas/doctor", _vm.AvgAppointmentsPerDoctor.ToString("N1"), "#e67e22");
                });

                col.Item().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.ConstantColumn(55);
                        c.ConstantColumn(65);
                        c.ConstantColumn(60);
                        c.ConstantColumn(55);
                        c.ConstantColumn(55);
                    });

                    // Header
                    table.Header(header =>
                    {
                        foreach (var h in new[] { "Doctor", "Especialidad", "Total", "Completadas", "Pendientes", "Canceladas", "No show" })
                        {
                            header.Cell()
                                .Background(HeaderColor)
                                .Padding(4)
                                .Text(h).Bold().FontColor("#FFFFFF").FontSize(8);
                        }
                    });

                    var rowIdx = 0;
                    foreach (var r in _vm.Rows.OrderByDescending(x => x.TotalAppointments))
                    {
                        var bg = rowIdx++ % 2 == 0 ? "#FFFFFF" : "#f8f9fa";
                        var pending = r.TotalAppointments - r.Completed - r.Cancelled - r.NoShow;

                        table.Cell().Background(bg).Padding(4).Text(r.DoctorName).FontSize(8);
                        table.Cell().Background(bg).Padding(4).Text(r.Speciality ?? "—").FontSize(8).FontColor("#6c757d");
                        table.Cell().Background(bg).Padding(4).AlignCenter().Text(r.TotalAppointments.ToString()).Bold().FontSize(8).FontColor("#2980b9");
                        table.Cell().Background(bg).Padding(4).AlignCenter().Text(r.Completed.ToString()).FontSize(8).FontColor("#27ae60");
                        table.Cell().Background(bg).Padding(4).AlignCenter().Text(pending > 0 ? pending.ToString() : "0").FontSize(8);
                        table.Cell().Background(bg).Padding(4).AlignCenter().Text(r.Cancelled.ToString()).FontSize(8).FontColor("#e74c3c");
                        table.Cell().Background(bg).Padding(4).AlignCenter().Text(r.NoShow.ToString()).FontSize(8).FontColor("#e67e22");
                    }
                });
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.Span("Página ").FontSize(8).FontColor("#adb5bd");
                t.CurrentPageNumber().FontSize(8).FontColor("#adb5bd");
                t.Span(" de ").FontSize(8).FontColor("#adb5bd");
                t.TotalPages().FontSize(8).FontColor("#adb5bd");
            });
        });
    }

    private static void SummaryCard(RowDescriptor row, string label, string value, string color)
    {
        row.RelativeItem().Border(1).BorderColor(color).Padding(8).Column(c =>
        {
            c.Item().Text(label).FontSize(7).FontColor("#6c757d").Bold();
            c.Item().Text(value).FontSize(18).Bold().FontColor(color);
        });
        row.ConstantItem(6);
    }
}
