using MedFlow.Application.Reporting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MedFlow.Web.Pdf;

public sealed class FinancialReportPdfDocument : IDocument
{
    private readonly string _clinicName;
    private readonly FinancialReportVm _report;
    private readonly DateTime? _from;
    private readonly DateTime? _to;

    private static readonly string HeaderColor = "#1a3c5e";
    private static readonly string GreenColor = "#27ae60";
    private static readonly string RedColor = "#e74c3c";

    public FinancialReportPdfDocument(string clinicName, FinancialReportVm report, DateTime? from, DateTime? to)
    {
        _clinicName = clinicName;
        _report = report;
        _from = from;
        _to = to;
    }

    public DocumentMetadata GetMetadata() => new DocumentMetadata
    {
        Title = $"Reporte Financiero — {_clinicName}",
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
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Helvetica"));

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
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(_clinicName).FontSize(16).Bold().FontColor(HeaderColor);
                col.Item().Text("REPORTE FINANCIERO").FontSize(11).FontColor(Colors.Grey.Darken2);
                var period = _from.HasValue || _to.HasValue
                    ? $"Período: {_from?.ToString("dd/MM/yyyy") ?? "—"} al {_to?.ToString("dd/MM/yyyy") ?? "—"}"
                    : "Período: Todos los registros";
                col.Item().Text(period).FontSize(9).FontColor(Colors.Grey.Darken1);
            });

            row.ConstantItem(120).Column(col =>
            {
                col.Item().Background(HeaderColor).Padding(8).AlignCenter()
                    .Text(DateTime.Now.ToString("dd/MM/yyyy")).FontSize(10).FontColor(Colors.White);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            // Summary cards
            col.Item().PaddingTop(12).Row(row =>
            {
                SummaryCard(row, "Facturado", _report.Summary.TotalInvoiced, HeaderColor);
                SummaryCard(row, "Cobrado", _report.Summary.TotalCollected, GreenColor);
                SummaryCard(row, "Pendiente", _report.Summary.Outstanding, _report.Summary.Outstanding > 0 ? RedColor : GreenColor);
                row.ConstantItem(120).Background("#f4f6f9").Padding(10).Column(c =>
                {
                    c.Item().Text("FACTURAS").FontSize(7).Bold().FontColor(Colors.Grey.Medium).AlignCenter();
                    c.Item().Text(_report.Summary.InvoiceCount.ToString()).FontSize(18).Bold().AlignCenter().FontColor(HeaderColor);
                });
            });

            // Payment methods breakdown
            if (_report.TotalsByPaymentMethod.Any())
            {
                col.Item().PaddingTop(16).Column(section =>
                {
                    section.Item().Text("COBROS POR MÉTODO DE PAGO").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                    section.Item().PaddingTop(4).Row(methodRow =>
                    {
                        foreach (var m in _report.TotalsByPaymentMethod)
                        {
                            methodRow.RelativeItem().Background("#f8f9fa").Padding(8).Column(c =>
                            {
                                c.Item().Text(m.Name).FontSize(8).FontColor(Colors.Grey.Darken2).AlignCenter();
                                c.Item().Text(m.Amount.ToString("N2")).FontSize(11).Bold().FontColor(HeaderColor).AlignCenter();
                            });
                        }
                    });
                });
            }

            // Invoices table
            if (_report.Invoices.Any())
            {
                col.Item().PaddingTop(16).Column(section =>
                {
                    section.Item().Text($"FACTURAS ({_report.Invoices.Count})").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                    section.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(80);  // Número
                            cols.RelativeColumn(3);   // Paciente
                            cols.ConstantColumn(70);  // Fecha
                            cols.ConstantColumn(70);  // Total
                            cols.ConstantColumn(70);  // Saldo
                            cols.ConstantColumn(55);  // Estado
                        });

                        static IContainer HCell(IContainer c) =>
                            c.Background(HeaderColor).Padding(4).AlignMiddle();

                        table.Header(h =>
                        {
                            h.Cell().Element(HCell).Text("N° Factura").FontColor(Colors.White).Bold().FontSize(7);
                            h.Cell().Element(HCell).Text("Paciente").FontColor(Colors.White).Bold().FontSize(7);
                            h.Cell().Element(HCell).AlignRight().Text("Fecha").FontColor(Colors.White).Bold().FontSize(7);
                            h.Cell().Element(HCell).AlignRight().Text("Total").FontColor(Colors.White).Bold().FontSize(7);
                            h.Cell().Element(HCell).AlignRight().Text("Saldo").FontColor(Colors.White).Bold().FontSize(7);
                            h.Cell().Element(HCell).AlignCenter().Text("Estado").FontColor(Colors.White).Bold().FontSize(7);
                        });

                        var rowIdx = 0;
                        foreach (var inv in _report.Invoices)
                        {
                            var bg = rowIdx++ % 2 == 0 ? "#FFFFFF" : "#f8f9fa";

                            static IContainer DCell(IContainer c, string bg) =>
                                c.Background(bg).BorderBottom(1).BorderColor("#dee2e6").Padding(4).AlignMiddle();

                            table.Cell().Element(c => DCell(c, bg)).Text(inv.InvoiceNumber).FontSize(8);
                            table.Cell().Element(c => DCell(c, bg)).Text(inv.PatientName).FontSize(8);
                            table.Cell().Element(c => DCell(c, bg)).AlignRight().Text(inv.IssueDate.ToString("dd/MM/yy")).FontSize(8);
                            table.Cell().Element(c => DCell(c, bg)).AlignRight().Text(inv.Total.ToString("N2")).FontSize(8);
                            table.Cell().Element(c => DCell(c, bg)).AlignRight()
                                .Text(inv.Balance.ToString("N2")).FontSize(8)
                                .FontColor(inv.Balance > 0 ? RedColor : Colors.Grey.Darken2);
                            table.Cell().Element(c => DCell(c, bg)).AlignCenter()
                                .Text(inv.StatusLabel).FontSize(7);
                        }
                    });
                });
            }

            // Payments table
            if (_report.Payments.Any())
            {
                col.Item().PaddingTop(16).Column(section =>
                {
                    section.Item().Text($"PAGOS RECIBIDOS ({_report.Payments.Count})").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                    section.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(70);  // Fecha
                            cols.RelativeColumn(3);   // Paciente
                            cols.ConstantColumn(80);  // Factura
                            cols.ConstantColumn(70);  // Monto
                            cols.ConstantColumn(70);  // Método
                        });

                        static IContainer HCell(IContainer c) =>
                            c.Background("#2c3e50").Padding(4).AlignMiddle();

                        table.Header(h =>
                        {
                            h.Cell().Element(HCell).Text("Fecha").FontColor(Colors.White).Bold().FontSize(7);
                            h.Cell().Element(HCell).Text("Paciente").FontColor(Colors.White).Bold().FontSize(7);
                            h.Cell().Element(HCell).Text("Factura").FontColor(Colors.White).Bold().FontSize(7);
                            h.Cell().Element(HCell).AlignRight().Text("Monto").FontColor(Colors.White).Bold().FontSize(7);
                            h.Cell().Element(HCell).Text("Método").FontColor(Colors.White).Bold().FontSize(7);
                        });

                        var rowIdx = 0;
                        foreach (var p in _report.Payments)
                        {
                            var bg = rowIdx++ % 2 == 0 ? "#FFFFFF" : "#f8f9fa";
                            static IContainer DCell(IContainer c, string bg) =>
                                c.Background(bg).BorderBottom(1).BorderColor("#dee2e6").Padding(4).AlignMiddle();

                            table.Cell().Element(c => DCell(c, bg)).Text(p.PaymentDate.ToString("dd/MM/yy")).FontSize(8);
                            table.Cell().Element(c => DCell(c, bg)).Text(p.PatientName).FontSize(8);
                            table.Cell().Element(c => DCell(c, bg)).Text(p.InvoiceNumber).FontSize(8);
                            table.Cell().Element(c => DCell(c, bg)).AlignRight()
                                .Text(p.Amount.ToString("N2")).FontSize(8).FontColor(GreenColor);
                            table.Cell().Element(c => DCell(c, bg)).Text(p.MethodLabel).FontSize(8);
                        }
                    });
                });
            }
        });
    }

    private static void SummaryCard(RowDescriptor row, string label, decimal value, string color)
    {
        row.RelativeItem().Background(color).Padding(10).Column(c =>
        {
            c.Item().Text(label).FontSize(7).Bold().FontColor(Colors.White).AlignCenter();
            c.Item().Text(value.ToString("N2")).FontSize(14).Bold().FontColor(Colors.White).AlignCenter();
        });
    }
}
