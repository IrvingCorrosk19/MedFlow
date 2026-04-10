using MedFlow.Application.Reporting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MedFlow.Web.Pdf;

public sealed class AppointmentsReportPdfDocument : IDocument
{
    private readonly string _clinicName;
    private readonly AppointmentsReportVm _report;
    private readonly DateTime? _from;
    private readonly DateTime? _to;

    private static readonly string HeaderColor = "#2c3e50";

    public AppointmentsReportPdfDocument(string clinicName, AppointmentsReportVm report, DateTime? from, DateTime? to)
    {
        _clinicName = clinicName;
        _report = report;
        _from = from;
        _to = to;
    }

    public DocumentMetadata GetMetadata() => new DocumentMetadata
    {
        Title = $"Reporte de Citas — {_clinicName}",
        Author = _clinicName,
        CreationDate = DateTimeOffset.UtcNow
    };

    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Helvetica"));

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(_clinicName).FontSize(15).Bold().FontColor(HeaderColor);
                        c.Item().Text("REPORTE DE CITAS").FontSize(10).FontColor(Colors.Grey.Darken2);
                        var period = _from.HasValue || _to.HasValue
                            ? $"Período: {_from?.ToString("dd/MM/yyyy") ?? "—"} al {_to?.ToString("dd/MM/yyyy") ?? "—"}"
                            : "Todos los registros";
                        c.Item().Text(period).FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                    row.ConstantItem(100).Column(c =>
                    {
                        c.Item().Background(HeaderColor).Padding(6).AlignCenter()
                            .Text(DateTime.Now.ToString("dd/MM/yyyy")).FontSize(9).FontColor(Colors.White);
                        c.Item().AlignCenter().PaddingTop(4)
                            .Text($"Total: {_report.TotalCount}").FontSize(9).Bold().FontColor(HeaderColor);
                    });
                });
                col.Item().PaddingTop(4).BorderBottom(2).BorderColor(HeaderColor);
            });

            page.Content().PaddingTop(10).Column(col =>
            {
                // Status summary
                if (_report.TotalsByStatus.Any())
                {
                    col.Item().Row(row =>
                    {
                        foreach (var s in _report.TotalsByStatus.Take(5))
                        {
                            row.RelativeItem().Background("#f4f6f9").Padding(6).Column(c =>
                            {
                                c.Item().Text(s.Name).FontSize(7).FontColor(Colors.Grey.Medium).AlignCenter();
                                c.Item().Text(s.Count.ToString()).FontSize(14).Bold().FontColor(HeaderColor).AlignCenter();
                            });
                        }
                    });
                }

                // Table
                col.Item().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(60);  // Fecha
                        cols.ConstantColumn(45);  // Hora
                        cols.RelativeColumn(3);   // Paciente
                        cols.RelativeColumn(2);   // Doctor
                        cols.ConstantColumn(55);  // Estado
                        cols.RelativeColumn(2);   // Motivo
                    });

                    static IContainer HCell(IContainer c) =>
                        c.Background(HeaderColor).Padding(4).AlignMiddle();

                    table.Header(h =>
                    {
                        h.Cell().Element(HCell).Text("Fecha").FontColor(Colors.White).Bold().FontSize(7);
                        h.Cell().Element(HCell).Text("Hora").FontColor(Colors.White).Bold().FontSize(7);
                        h.Cell().Element(HCell).Text("Paciente").FontColor(Colors.White).Bold().FontSize(7);
                        h.Cell().Element(HCell).Text("Doctor").FontColor(Colors.White).Bold().FontSize(7);
                        h.Cell().Element(HCell).AlignCenter().Text("Estado").FontColor(Colors.White).Bold().FontSize(7);
                        h.Cell().Element(HCell).Text("Motivo").FontColor(Colors.White).Bold().FontSize(7);
                    });

                    var rowIdx = 0;
                    foreach (var r in _report.Rows)
                    {
                        var bg = rowIdx++ % 2 == 0 ? "#FFFFFF" : "#f8f9fa";
                        static IContainer DCell(IContainer c, string bg) =>
                            c.Background(bg).BorderBottom(1).BorderColor("#dee2e6").Padding(4).AlignMiddle();

                        table.Cell().Element(c => DCell(c, bg)).Text(r.Date.ToString("dd/MM/yy")).FontSize(8);
                        table.Cell().Element(c => DCell(c, bg)).Text(r.Start.ToString(@"hh\:mm")).FontSize(8);
                        table.Cell().Element(c => DCell(c, bg)).Text(r.PatientName).FontSize(8);
                        table.Cell().Element(c => DCell(c, bg)).Text(r.DoctorName).FontSize(8);
                        table.Cell().Element(c => DCell(c, bg)).AlignCenter().Text(r.StatusLabel).FontSize(7);
                        table.Cell().Element(c => DCell(c, bg)).Text(r.Reason ?? "—").FontSize(8);
                    }
                });
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Pág. ").FontSize(8).FontColor(Colors.Grey.Medium);
                x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                x.Span(" / ").FontSize(8).FontColor(Colors.Grey.Medium);
                x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                x.Span($"  |  {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });
    }
}
