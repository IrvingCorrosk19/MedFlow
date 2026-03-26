using MedFlow.Application.Reporting;

namespace MedFlow.Application.Interfaces;

public interface IExecutiveAnalyticsService
{
    Task<ExecutiveDashboardVm> GetExecutiveDashboardAsync(ExecutiveDashboardFilter filter, CancellationToken cancellationToken = default);

    Task<AppointmentsReportVm> GetAppointmentsReportAsync(AppointmentsReportFilter filter, CancellationToken cancellationToken = default);

    Task<PatientsReportVm> GetPatientsReportAsync(PatientsReportFilter filter, CancellationToken cancellationToken = default);

    Task<FinancialReportVm> GetFinancialReportAsync(FinancialReportFilter filter, CancellationToken cancellationToken = default);

    Task<DoctorsReportVm> GetDoctorsReportAsync(DoctorsReportFilter filter, CancellationToken cancellationToken = default);
}
