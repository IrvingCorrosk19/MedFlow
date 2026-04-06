using System.Text;
using MedFlow.Application.Accounting;
using MedFlow.Application.Interfaces;

namespace MedFlow.Infrastructure.Services;

public class AccountingExportService : IAccountingExportService
{
    // ── CSS shared by all HTML exports ─────────────────────────────────────────

    private const string PrintCss = """
        body { font-family: Arial, sans-serif; font-size: 12px; margin: 20px; }
        h2 { margin-bottom: 4px; }
        p.subtitle { margin-top: 0; color: #555; }
        table { width: 100%; border-collapse: collapse; margin-top: 12px; }
        th, td { border: 1px solid #ccc; padding: 4px 8px; }
        th { background: #f0f0f0; }
        .text-right { text-align: right; }
        .font-bold { font-weight: bold; }
        .section-header td { background: #e8e8e8; font-weight: bold; }
        .total-row td { background: #f5f5f5; font-weight: bold; }
        .grand-total td { background: #d0d0d0; font-weight: bold; }
        @media print { .no-print { display: none; } }
        """;

    // ── CSV exports ────────────────────────────────────────────────────────────

    public string ExportTrialBalanceCsv(TrialBalanceDto data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Código,Nombre,Tipo,Débito,Crédito");
        foreach (var l in data.Lines)
        {
            sb.AppendLine($"{CsvEscape(l.AccountCode)},{CsvEscape(l.AccountName)},{l.AccountType},{l.DebitBalance:N2},{l.CreditBalance:N2}");
        }
        sb.AppendLine($",,TOTALES,{data.TotalDebit:N2},{data.TotalCredit:N2}");
        return sb.ToString();
    }

    public string ExportBalanceSheetCsv(BalanceSheetDto data)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Balance General — Al {data.AsOf:dd/MM/yyyy}");
        sb.AppendLine();
        sb.AppendLine("Sección,Código,Cuenta,Saldo");

        sb.AppendLine("ACTIVOS,,,");
        foreach (var a in data.Assets)
            sb.AppendLine($"Activos,{CsvEscape(a.AccountCode)},{CsvEscape(a.AccountName)},{a.Balance:N2}");
        sb.AppendLine($"TOTAL ACTIVOS,,,{data.TotalAssets:N2}");

        sb.AppendLine();
        sb.AppendLine("PASIVOS,,,");
        foreach (var l in data.Liabilities)
            sb.AppendLine($"Pasivos,{CsvEscape(l.AccountCode)},{CsvEscape(l.AccountName)},{l.Balance:N2}");
        sb.AppendLine($"TOTAL PASIVOS,,,{data.TotalLiabilities:N2}");

        sb.AppendLine();
        sb.AppendLine("PATRIMONIO,,,");
        foreach (var e in data.Equity)
            sb.AppendLine($"Patrimonio,{CsvEscape(e.AccountCode)},{CsvEscape(e.AccountName)},{e.Balance:N2}");
        sb.AppendLine($"TOTAL PATRIMONIO,,,{data.TotalEquity:N2}");

        sb.AppendLine();
        sb.AppendLine($"TOTAL PASIVOS + PATRIMONIO,,,{(data.TotalLiabilities + data.TotalEquity):N2}");

        return sb.ToString();
    }

    public string ExportIncomeStatementCsv(IncomeStatementDto data)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Estado de Resultados — {data.From:dd/MM/yyyy} al {data.To:dd/MM/yyyy}");
        sb.AppendLine();
        sb.AppendLine("Sección,Código,Cuenta,Monto");

        sb.AppendLine("INGRESOS,,,");
        foreach (var r in data.Revenue)
            sb.AppendLine($"Ingresos,{CsvEscape(r.AccountCode)},{CsvEscape(r.AccountName)},{r.Amount:N2}");
        sb.AppendLine($"TOTAL INGRESOS,,,{data.TotalRevenue:N2}");

        if (data.Costs.Any())
        {
            sb.AppendLine();
            sb.AppendLine("COSTO DE VENTAS,,,");
            foreach (var c in data.Costs)
                sb.AppendLine($"Costos,{CsvEscape(c.AccountCode)},{CsvEscape(c.AccountName)},{c.Amount:N2}");
            sb.AppendLine($"TOTAL COSTOS,,,{data.TotalCosts:N2}");
        }

        sb.AppendLine();
        sb.AppendLine($"UTILIDAD BRUTA,,,{data.GrossProfit:N2}");

        if (data.Expenses.Any())
        {
            sb.AppendLine();
            sb.AppendLine("GASTOS OPERATIVOS,,,");
            foreach (var e in data.Expenses)
                sb.AppendLine($"Gastos,{CsvEscape(e.AccountCode)},{CsvEscape(e.AccountName)},{e.Amount:N2}");
            sb.AppendLine($"TOTAL GASTOS,,,{data.TotalExpenses:N2}");
        }

        sb.AppendLine();
        sb.AppendLine($"UTILIDAD NETA,,,{data.NetIncome:N2}");

        return sb.ToString();
    }

    // ── HTML exports ───────────────────────────────────────────────────────────

    public byte[] ExportTrialBalanceHtml(TrialBalanceDto data)
    {
        var rows = new StringBuilder();
        foreach (var l in data.Lines)
        {
            rows.AppendLine($"""
                <tr>
                  <td>{HtmlEncode(l.AccountCode)}</td>
                  <td>{HtmlEncode(l.AccountName)}</td>
                  <td>{l.AccountType}</td>
                  <td class="text-right">{(l.DebitBalance > 0 ? l.DebitBalance.ToString("N2") : "—")}</td>
                  <td class="text-right">{(l.CreditBalance > 0 ? l.CreditBalance.ToString("N2") : "—")}</td>
                </tr>
                """);
        }

        var balanceStatus = data.TotalDebit == data.TotalCredit
            ? "<p style=\"color:green;font-weight:bold\">&#10003; Balance cuadrado correctamente</p>"
            : $"<p style=\"color:red;font-weight:bold\">&#9888; DIFERENCIA: {Math.Abs(data.TotalDebit - data.TotalCredit):N2}</p>";

        var html = $"""
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8">
            <title>Balance de Comprobación</title>
            <style>{PrintCss}</style>
            </head>
            <body>
            <h2>Balance de Comprobación</h2>
            <p class="subtitle">Al: {data.AsOf:dd/MM/yyyy}</p>
            <table>
              <thead>
                <tr>
                  <th>Código</th>
                  <th>Cuenta</th>
                  <th>Tipo</th>
                  <th class="text-right">Débito</th>
                  <th class="text-right">Crédito</th>
                </tr>
              </thead>
              <tbody>
            {rows}
              </tbody>
              <tfoot>
                <tr class="total-row">
                  <td colspan="3" class="font-bold text-right">TOTALES</td>
                  <td class="text-right font-bold">{data.TotalDebit:N2}</td>
                  <td class="text-right font-bold">{data.TotalCredit:N2}</td>
                </tr>
              </tfoot>
            </table>
            {balanceStatus}
            </body>
            </html>
            """;

        return Encoding.UTF8.GetBytes(html);
    }

    public byte[] ExportBalanceSheetHtml(BalanceSheetDto data)
    {
        var assetRows = BuildSectionRows(data.Assets);
        var liabilityRows = BuildSectionRows(data.Liabilities);
        var equityRows = BuildSectionRows(data.Equity);

        var balanced = data.TotalAssets == data.TotalLiabilities + data.TotalEquity;
        var balanceStatus = balanced
            ? "<p style=\"color:green;font-weight:bold\">&#10003; Balance cuadrado: Activos = Pasivos + Patrimonio</p>"
            : $"<p style=\"color:red;font-weight:bold\">&#9888; DIFERENCIA: {Math.Abs(data.TotalAssets - data.TotalLiabilities - data.TotalEquity):N2}</p>";

        var html = $"""
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8">
            <title>Balance General</title>
            <style>{PrintCss}</style>
            </head>
            <body>
            <h2>Balance General</h2>
            <p class="subtitle">Al: {data.AsOf:dd/MM/yyyy}</p>

            <table>
              <thead>
                <tr><th colspan="2">ACTIVOS</th></tr>
              </thead>
              <tbody>
            {assetRows}
              </tbody>
              <tfoot>
                <tr class="total-row">
                  <td class="font-bold">TOTAL ACTIVOS</td>
                  <td class="text-right font-bold">{data.TotalAssets:N2}</td>
                </tr>
              </tfoot>
            </table>

            <table style="margin-top:16px">
              <thead>
                <tr><th colspan="2">PASIVOS</th></tr>
              </thead>
              <tbody>
            {liabilityRows}
              </tbody>
              <tfoot>
                <tr class="total-row">
                  <td class="font-bold">TOTAL PASIVOS</td>
                  <td class="text-right font-bold">{data.TotalLiabilities:N2}</td>
                </tr>
              </tfoot>
            </table>

            <table style="margin-top:16px">
              <thead>
                <tr><th colspan="2">PATRIMONIO</th></tr>
              </thead>
              <tbody>
            {equityRows}
              </tbody>
              <tfoot>
                <tr class="total-row">
                  <td class="font-bold">TOTAL PATRIMONIO</td>
                  <td class="text-right font-bold">{data.TotalEquity:N2}</td>
                </tr>
                <tr class="grand-total">
                  <td class="font-bold">TOTAL PASIVOS + PATRIMONIO</td>
                  <td class="text-right font-bold">{(data.TotalLiabilities + data.TotalEquity):N2}</td>
                </tr>
              </tfoot>
            </table>

            {balanceStatus}
            </body>
            </html>
            """;

        return Encoding.UTF8.GetBytes(html);
    }

    public byte[] ExportIncomeStatementHtml(IncomeStatementDto data)
    {
        var revenueRows = BuildIncomeRows(data.Revenue);
        var costsSection = data.Costs.Any()
            ? $"""
               <tr class="section-header"><td colspan="2">COSTO DE VENTAS</td></tr>
               {BuildIncomeRows(data.Costs)}
               <tr class="total-row"><td class="font-bold">Total Costos</td><td class="text-right font-bold">({data.TotalCosts:N2})</td></tr>
               """
            : string.Empty;

        var expensesSection = data.Expenses.Any()
            ? $"""
               <tr class="section-header"><td colspan="2">GASTOS OPERATIVOS</td></tr>
               {BuildIncomeRows(data.Expenses)}
               <tr class="total-row"><td class="font-bold">Total Gastos</td><td class="text-right font-bold">({data.TotalExpenses:N2})</td></tr>
               """
            : string.Empty;

        var html = $"""
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8">
            <title>Estado de Resultados</title>
            <style>{PrintCss}</style>
            </head>
            <body>
            <h2>Estado de Resultados</h2>
            <p class="subtitle">{data.From:dd/MM/yyyy} — {data.To:dd/MM/yyyy}</p>

            <table>
              <tbody>
                <tr class="section-header"><td colspan="2">INGRESOS</td></tr>
            {revenueRows}
                <tr class="total-row"><td class="font-bold">Total Ingresos</td><td class="text-right font-bold">{data.TotalRevenue:N2}</td></tr>
            {costsSection}
                <tr class="grand-total"><td class="font-bold">UTILIDAD BRUTA</td><td class="text-right font-bold">{data.GrossProfit:N2}</td></tr>
            {expensesSection}
                <tr class="grand-total" style="font-size:1.1em"><td class="font-bold">UTILIDAD NETA</td><td class="text-right font-bold">{data.NetIncome:N2}</td></tr>
              </tbody>
            </table>
            </body>
            </html>
            """;

        return Encoding.UTF8.GetBytes(html);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string BuildSectionRows(IEnumerable<BalanceSheetSectionDto> items)
    {
        var sb = new StringBuilder();
        foreach (var item in items)
            sb.AppendLine($"<tr><td>{HtmlEncode(item.AccountCode)} {HtmlEncode(item.AccountName)}</td><td class=\"text-right\">{item.Balance:N2}</td></tr>");
        return sb.ToString();
    }

    private static string BuildIncomeRows(IEnumerable<IncomeStatementLineDto> items)
    {
        var sb = new StringBuilder();
        foreach (var item in items)
            sb.AppendLine($"<tr><td style=\"padding-left:16px\">{HtmlEncode(item.AccountCode)} {HtmlEncode(item.AccountName)}</td><td class=\"text-right\">{item.Amount:N2}</td></tr>");
        return sb.ToString();
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string HtmlEncode(string value)
        => System.Net.WebUtility.HtmlEncode(value);
}
