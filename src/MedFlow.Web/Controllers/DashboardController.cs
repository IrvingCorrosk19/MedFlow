using MedFlow.Application.Interfaces;
using MedFlow.Application.Reporting;
using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePermission(PermissionCodes.DashboardView)]
public class DashboardController : Controller
{
    private readonly IExecutiveAnalyticsService _analytics;

    public DashboardController(IExecutiveAnalyticsService analytics)
    {
        _analytics = analytics;
    }

    public async Task<IActionResult> ExportCsv(int days = 14, CancellationToken cancellationToken = default)
    {
        var clampedDays = Math.Clamp(days, 1, 365);
        var model = await _analytics.GetExecutiveDashboardAsync(new ExecutiveDashboardFilter(clampedDays), cancellationToken);
        if (model == null) return NotFound();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Sección,Indicador,Valor");
        sb.AppendLine($"Citas,Total hoy,{model.AppointmentKpis.TotalToday}");
        sb.AppendLine($"Citas,Pendientes hoy,{model.AppointmentKpis.PendingToday}");
        sb.AppendLine($"Citas,Confirmadas hoy,{model.AppointmentKpis.ConfirmedToday}");
        sb.AppendLine($"Citas,Completadas hoy,{model.AppointmentKpis.CompletedToday}");
        sb.AppendLine($"Citas,Canceladas hoy,{model.AppointmentKpis.CancelledToday}");
        sb.AppendLine($"Citas,No-show hoy,{model.AppointmentKpis.NoShowToday}");
        sb.AppendLine($"Citas,Tasa cancelación período (%),{model.CancellationRatePeriod}");
        sb.AppendLine($"Citas,Tasa completitud período (%),{model.CompletionRatePeriod}");
        sb.AppendLine($"Pacientes,Total registrados,{model.PatientKpis.TotalRegistered}");
        sb.AppendLine($"Pacientes,Nuevos este mes,{model.PatientKpis.NewThisMonth}");
        sb.AppendLine($"Pacientes,Atendidos hoy,{model.PatientKpis.DistinctAttendedToday}");
        sb.AppendLine($"Finanzas,Facturado hoy,{model.FinanceKpis.BillingToday}");
        sb.AppendLine($"Finanzas,Cobrado hoy,{model.FinanceKpis.PaymentsToday}");
        sb.AppendLine($"Finanzas,Facturado mes,{model.FinanceKpis.BillingMonth}");
        sb.AppendLine($"Finanzas,Saldo pendiente,{model.FinanceKpis.TotalOutstanding}");
        sb.AppendLine();
        sb.AppendLine("Citas por día (período)");
        sb.AppendLine("Fecha,Citas");
        foreach (var r in model.AppointmentsByDay)
            sb.AppendLine($"{r.Date:dd/MM/yyyy},{r.Count}");
        sb.AppendLine();
        sb.AppendLine("Ingresos por día (período)");
        sb.AppendLine("Fecha,Monto");
        foreach (var r in model.RevenueByDay)
            sb.AppendLine($"{r.Date:dd/MM/yyyy},{r.Amount}");

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(
            System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"dashboard_{DateTime.Today:yyyyMMdd}_{clampedDays}d.csv");
    }

    public async Task<IActionResult> Index(int days = 14, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Dashboard ejecutivo";
        ViewData["PageSubtitle"] = "KPIs, tendencias y operación clínica";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Dashboard</li>";

        var clampedDays = Math.Clamp(days, 1, 365);
        ViewBag.Days = clampedDays;

        try
        {
            var model = await _analytics.GetExecutiveDashboardAsync(new ExecutiveDashboardFilter(clampedDays), cancellationToken);
            return View(model);
        }
        catch (Exception ex)
        {
            ViewData["ErrorMessage"] = "No se pudieron cargar los datos del dashboard. Intente de nuevo en unos momentos.";
            Microsoft.Extensions.Logging.LoggerExtensions.LogError(
                HttpContext.RequestServices.GetRequiredService<ILogger<DashboardController>>(), ex, "Error cargando dashboard");
            return View(null as MedFlow.Application.Reporting.ExecutiveDashboardVm);
        }
    }
}
