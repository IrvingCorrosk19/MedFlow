using MedFlow.Application.Accounting;

namespace MedFlow.Application.Interfaces;

public interface IAccountingExportService
{
    string ExportTrialBalanceCsv(TrialBalanceDto data);
    string ExportBalanceSheetCsv(BalanceSheetDto data);
    string ExportIncomeStatementCsv(IncomeStatementDto data);
    byte[] ExportTrialBalanceHtml(TrialBalanceDto data);
    byte[] ExportBalanceSheetHtml(BalanceSheetDto data);
    byte[] ExportIncomeStatementHtml(IncomeStatementDto data);
}
