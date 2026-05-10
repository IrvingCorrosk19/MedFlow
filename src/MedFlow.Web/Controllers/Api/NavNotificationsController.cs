using MedFlow.Application.Interfaces;
using MedFlow.Domain.Enums;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Web.Controllers.Api;

/// <summary>
/// Lightweight endpoint for the navbar notification bell.
/// Returns upcoming appointments and overdue invoices in a single call.
/// </summary>
[Authorize(Roles = MedFlowStaffRoles.List)]
[Route("api/nav/notifications")]
[ApiController]
public class NavNotificationsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IClinicalUserScope _clinicalScope;

    public NavNotificationsController(IApplicationDbContext db, ITenantContext tenant, IClinicalUserScope clinicalScope)
    {
        _db = db;
        _tenant = tenant;
        _clinicalScope = clinicalScope;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return Ok(new { count = 0, items = Array.Empty<object>() });
        var tid = _tenant.TenantId.Value;
        var now = DateTime.UtcNow;
        var today = now.Date;
        var tomorrow = today.AddDays(1);

        var items = new List<object>();
        var (doctorScoped, linkedDoctorId) = await _clinicalScope.GetDoctorDataScopeAsync(ct);

        // Appointments today
        var todayQ = _db.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Where(a => a.TenantId == tid
                && a.Status == AppointmentStatus.Scheduled
                && a.ScheduledDate >= today
                && a.ScheduledDate < tomorrow
                && !a.IsDeleted);
        if (doctorScoped)
        {
            if (!linkedDoctorId.HasValue)
                todayQ = todayQ.Where(a => false);
            else
                todayQ = todayQ.Where(a => a.DoctorId == linkedDoctorId.Value);
        }

        var todayApts = await todayQ
            .OrderBy(a => a.ScheduledDate)
            .Take(5)
            .ToListAsync(ct);

        foreach (var a in todayApts)
        {
            items.Add(new
            {
                icon = "fa fa-calendar text-info",
                text = $"Cita hoy {a.ScheduledDate:HH:mm} — {a.Patient?.NombreCompleto ?? "Paciente"}",
                link = $"/Appointments/Details/{a.Id}",
                type = "appointment"
            });
        }

        // Overdue invoices — solo roles no limitados a directorio médico (evita filtración PHI por factura)
        if (doctorScoped)
            goto SkipFinanceNotifications;

        // Overdue invoices (Pending or PartiallyPaid with DueDate in the past)
        var overdueInvoices = await _db.BillingInvoices
            .AsNoTracking()
            .Include(i => i.Patient)
            .Where(i => i.TenantId == tid
                && (i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.PartiallyPaid)
                && i.DueDate.HasValue && i.DueDate.Value < now
                && !i.IsDeleted)
            .OrderBy(i => i.DueDate)
            .Take(3)
            .ToListAsync(ct);

        foreach (var inv in overdueInvoices)
        {
            items.Add(new
            {
                icon = "fa fa-exclamation-circle text-danger",
                text = $"Factura vencida {inv.InvoiceNumber} — {inv.Patient?.NombreCompleto ?? ""}",
                link = $"/BillingInvoices/Details/{inv.Id}",
                type = "invoice"
            });
        }

        SkipFinanceNotifications:

        // Appointments tomorrow (preview)
        var tomorrowQ = _db.Appointments
            .AsNoTracking()
            .Where(a => a.TenantId == tid
                && a.Status == AppointmentStatus.Scheduled
                && a.ScheduledDate >= tomorrow
                && a.ScheduledDate < tomorrow.AddDays(1)
                && !a.IsDeleted);
        if (doctorScoped && linkedDoctorId.HasValue)
            tomorrowQ = tomorrowQ.Where(a => a.DoctorId == linkedDoctorId.Value);
        else if (doctorScoped)
            tomorrowQ = tomorrowQ.Where(a => false);

        var tomorrowCount = await tomorrowQ.CountAsync(ct);

        if (tomorrowCount > 0)
        {
            items.Add(new
            {
                icon = "fa fa-clock-o text-warning",
                text = $"{tomorrowCount} cita(s) mañana",
                link = $"/Appointments?date={tomorrow:yyyy-MM-dd}",
                type = "reminder"
            });
        }

        return Ok(new { count = items.Count, items });
    }
}
