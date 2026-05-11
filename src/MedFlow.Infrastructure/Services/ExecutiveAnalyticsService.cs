using MedFlow.Application.Interfaces;
using MedFlow.Application.Reporting;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public class ExecutiveAnalyticsService : IExecutiveAnalyticsService
{
    private readonly ApplicationDbContext _db;

    public ExecutiveAnalyticsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ExecutiveDashboardVm> GetExecutiveDashboardAsync(ExecutiveDashboardFilter filter, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var todayEnd = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var chartDays = Math.Clamp(filter.ChartDays, 7, 90);
        var chartFrom = today.AddDays(-(chartDays - 1));
        var utcDayStart = DateTime.UtcNow.Date;
        var utcDayEnd = utcDayStart.AddDays(1);
        var monthStartUtc = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEndUtc = monthStartUtc.AddMonths(1);

        var aptBase = _db.Appointments.AsNoTracking().Where(a => !a.IsDeleted);

        var aptTodayQuery = aptBase.Where(a => a.ScheduledDate >= today && a.ScheduledDate < todayEnd);
        var statusToday = await aptTodayQuery
            .GroupBy(a => a.Status)
            .Select(g => new { g.Key, C = g.Count() })
            .ToListAsync(cancellationToken);
        int C(AppointmentStatus s) => statusToday.FirstOrDefault(x => x.Key == s)?.C ?? 0;

        var appointmentKpis = new AppointmentKpiVm(
            TotalToday: await aptTodayQuery.CountAsync(cancellationToken),
            PendingToday: C(AppointmentStatus.Scheduled),
            ConfirmedToday: C(AppointmentStatus.Confirmed),
            CancelledToday: C(AppointmentStatus.Cancelled),
            CompletedToday: C(AppointmentStatus.Completed),
            NoShowToday: C(AppointmentStatus.NoShow));

        var patientKpis = new PatientKpiVm(
            TotalRegistered: await _db.Patients.AsNoTracking().CountAsync(p => !p.IsDeleted, cancellationToken),
            NewThisMonth: await _db.Patients.AsNoTracking()
                .CountAsync(p => !p.IsDeleted && p.CreatedAt >= monthStartUtc && p.CreatedAt < monthEndUtc, cancellationToken),
            DistinctAttendedToday: await aptBase
                .Where(a => a.ScheduledDate >= today && a.ScheduledDate < todayEnd && a.Status == AppointmentStatus.Completed)
                .Select(a => a.PatientId).Distinct().CountAsync(cancellationToken),
            InactiveCount: await _db.Patients.AsNoTracking().CountAsync(p => !p.IsDeleted && !p.IsActive, cancellationToken));

        var periodApts = aptBase.Where(a => a.ScheduledDate >= chartFrom && a.ScheduledDate < todayEnd);
        var topDoc = await periodApts
            .GroupBy(a => new { a.DoctorId, Name = a.Doctor!.FirstName + " " + a.Doctor.LastName })
            .Select(g => new { g.Key.Name, C = g.Count() })
            .OrderByDescending(x => x.C)
            .FirstOrDefaultAsync(cancellationToken);

        var topSpec = await periodApts
            .Where(a => a.Doctor != null && a.Doctor.Speciality != null && a.Doctor.Speciality != "")
            .GroupBy(a => a.Doctor!.Speciality!)
            .Select(g => new { Spec = g.Key, C = g.Count() })
            .OrderByDescending(x => x.C)
            .FirstOrDefaultAsync(cancellationToken);

        var doctorKpis = new DoctorKpiVm(
            ActiveCount: await _db.Doctors.AsNoTracking().CountAsync(d => !d.IsDeleted && d.IsActive, cancellationToken),
            TopDoctorByAppointmentsName: topDoc?.Name,
            TopDoctorAppointmentCount: topDoc?.C ?? 0,
            TopSpecialityName: topSpec?.Spec,
            TopSpecialityAppointmentCount: topSpec?.C ?? 0);

        var invBase = _db.BillingInvoices.AsNoTracking().Where(i => !i.IsDeleted);
        var payBase = _db.Payments.AsNoTracking().Where(p => !p.IsDeleted && p.Status == PaymentEntryStatus.Registered);

        var billingToday = await invBase
            .Where(i => i.IssueDate >= utcDayStart && i.IssueDate < utcDayEnd && i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => i.TotalAmount, cancellationToken);
        var billingMonth = await invBase
            .Where(i => i.IssueDate >= monthStartUtc && i.IssueDate < monthEndUtc && i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => i.TotalAmount, cancellationToken);
        var paymentsToday = await payBase
            .Where(p => p.PaymentDate >= utcDayStart && p.PaymentDate < utcDayEnd)
            .SumAsync(p => p.Amount, cancellationToken);
        var outstanding = await invBase
            .Where(i => i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => i.BalanceDue, cancellationToken);
        var pendInv = await invBase.CountAsync(i =>
            i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.PartiallyPaid, cancellationToken);
        var paidInv = await invBase.CountAsync(i => i.Status == InvoiceStatus.Paid, cancellationToken);

        var aptsByDayRaw = await aptBase
            .Where(a => a.ScheduledDate >= chartFrom && a.ScheduledDate < todayEnd)
            .GroupBy(a => a.ScheduledDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Count(), Cancelled = g.Count(x => x.Status == AppointmentStatus.Cancelled) })
            .ToListAsync(cancellationToken);
        var aptsByDayMap = aptsByDayRaw.ToDictionary(x => x.Date, x => (x.Total, x.Cancelled));
        var appointmentsByDay = Enumerable.Range(0, chartDays)
            .Select(i => chartFrom.AddDays(i))
            .Select(d => new DateCountVm(d, aptsByDayMap.TryGetValue(d, out var vd) ? vd.Total : 0))
            .ToList();

        var periodForRates = aptBase.Where(a => a.ScheduledDate >= chartFrom && a.ScheduledDate < todayEnd);
        var totalPeriod = await periodForRates.CountAsync(cancellationToken);
        var cancelledPeriod = await periodForRates.CountAsync(a => a.Status == AppointmentStatus.Cancelled, cancellationToken);
        var completedPeriod = await periodForRates.CountAsync(a => a.Status == AppointmentStatus.Completed, cancellationToken);
        var cancelRate = totalPeriod == 0 ? 0 : Math.Round(100m * cancelledPeriod / totalPeriod, 1);
        var completeRate = totalPeriod == 0 ? 0 : Math.Round(100m * completedPeriod / totalPeriod, 1);

        var appointmentsByStatusRaw = await periodForRates
            .GroupBy(a => a.Status)
            .Select(g => new { Name = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(cancellationToken);
        var appointmentsByStatus = appointmentsByStatusRaw
            .Select(x => new NamedCountVm(x.Name, x.Count))
            .ToList();

        var payFromUtcForChart = DateTime.SpecifyKind(chartFrom, DateTimeKind.Utc);
        var revenueByDayRaw = await payBase
            .Where(p => p.PaymentDate >= payFromUtcForChart && p.PaymentDate < utcDayEnd)
            .GroupBy(p => p.PaymentDate.Date)
            .Select(g => new { Date = g.Key, Sum = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);
        var revenueByDayMap = revenueByDayRaw.ToDictionary(x => x.Date, x => x.Sum);
        var revenueByDay = Enumerable.Range(0, chartDays)
            .Select(i => chartFrom.AddDays(i))
            .Select(d => new DateDecimalVm(d, revenueByDayMap.GetValueOrDefault(d, 0m)))
            .ToList();

        var revenuePeriodTotal = revenueByDay.Sum(x => x.Amount);
        decimal? avgCollectedPerCompleted = completedPeriod > 0
            ? Math.Round(revenuePeriodTotal / completedPeriod, 2)
            : null;

        var financeKpis = new FinanceKpiVm(
            BillingToday: billingToday,
            BillingMonth: billingMonth,
            PaymentsToday: paymentsToday,
            TotalOutstanding: outstanding,
            InvoicesPendingOrPartial: pendInv,
            InvoicesPaid: paidInv,
            AvgCollectedPerCompletedAppointmentPeriod: avgCollectedPerCompleted);

        var twelveMonthsAgo = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);
        var patientsByMonthRaw = await _db.Patients.AsNoTracking()
            .Where(p => !p.IsDeleted && p.CreatedAt >= twelveMonthsAgo)
            .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var patientsByMonthMap = patientsByMonthRaw.ToDictionary(x => (x.Year, x.Month), x => x.Count);
        var newPatientsByMonth = Enumerable.Range(0, 12)
            .Select(i => DateTime.UtcNow.AddMonths(-(11 - i)))
            .Select(dt => new MonthCountVm(dt.Year, dt.Month, patientsByMonthMap.GetValueOrDefault((dt.Year, dt.Month), 0)))
            .ToList();

        var topSpecsRaw = await periodApts
            .Where(a => a.Doctor != null && !string.IsNullOrEmpty(a.Doctor.Speciality))
            .GroupBy(a => a.Doctor!.Speciality!)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToListAsync(cancellationToken);
        var topSpecs = topSpecsRaw
            .Select(x => new NamedCountVm(x.Name, x.Count))
            .ToList();

        var topDocsRaw = await periodApts
            .GroupBy(a => new { a.DoctorId, Name = a.Doctor!.FirstName + " " + a.Doctor.LastName })
            .Select(g => new { Name = g.Key.Name, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToListAsync(cancellationToken);
        var topDocs = topDocsRaw
            .Select(x => new NamedCountVm(x.Name, x.Count))
            .ToList();

        var payFromUtc = DateTime.UtcNow.Date.AddDays(-(chartDays - 1));
        var payMethods = await payBase
            .Where(p => p.PaymentDate >= payFromUtc && p.PaymentDate < utcDayEnd)
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new NamedDecimalVm(g.Key.ToString(), g.Sum(x => x.Amount)))
            .ToListAsync(cancellationToken);

        var cancelByDay = Enumerable.Range(0, chartDays)
            .Select(i => chartFrom.AddDays(i))
            .Select(d => new DateCountVm(d, aptsByDayMap.TryGetValue(d, out var vc) ? vc.Cancelled : 0))
            .ToList();

        var recentActivity = await BuildRecentActivityAsync(cancellationToken);
        var alerts = await BuildExecutiveAlertsAsync(today, todayEnd, utcDayStart, utcDayEnd, cancellationToken);

        var upcoming = await aptBase
            .Where(a => a.ScheduledDate >= today && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.Completed)
            .OrderBy(a => a.ScheduledDate).ThenBy(a => a.StartTime)
            .Take(12)
            .Select(a => new UpcomingAppointmentVm(
                a.Id,
                a.ScheduledDate,
                a.StartTime,
                a.EndTime,
                a.Patient!.NombreCompleto,
                a.Doctor!.FirstName + " " + a.Doctor.LastName,
                a.Status.ToString()))
            .ToListAsync(cancellationToken);

        return new ExecutiveDashboardVm(
            appointmentKpis,
            patientKpis,
            doctorKpis,
            financeKpis,
            appointmentsByDay,
            appointmentsByStatus,
            revenueByDay,
            newPatientsByMonth,
            topSpecs,
            topDocs,
            payMethods,
            cancelByDay,
            cancelRate,
            completeRate,
            recentActivity,
            alerts,
            upcoming);
    }

    private async Task<List<ExecutiveAlertVm>> BuildExecutiveAlertsAsync(
        DateTime today, DateTime todayEnd, DateTime utcDayStart, DateTime utcDayEnd, CancellationToken cancellationToken)
    {
        var list = new List<ExecutiveAlertVm>();

        var cancelledToday = await _db.Appointments.AsNoTracking()
            .CountAsync(a => !a.IsDeleted && a.ScheduledDate >= today && a.ScheduledDate < todayEnd &&
                a.Status == AppointmentStatus.Cancelled, cancellationToken);
        if (cancelledToday > 0)
            list.Add(new ExecutiveAlertVm("warning", $"{cancelledToday} cita(s) cancelada(s) hoy."));

        var overdue = await _db.BillingInvoices.AsNoTracking()
            .CountAsync(i => !i.IsDeleted && i.Status != InvoiceStatus.Cancelled && i.BalanceDue > 0 &&
                i.DueDate != null && i.DueDate < DateTime.UtcNow.Date, cancellationToken);
        if (overdue > 0)
            list.Add(new ExecutiveAlertVm("danger", $"{overdue} factura(s) con saldo y fecha de vencimiento pasada."));

        var openBalance = await _db.BillingInvoices.AsNoTracking()
            .AnyAsync(i => !i.IsDeleted && i.Status != InvoiceStatus.Cancelled && i.BalanceDue > 0, cancellationToken);
        if (openBalance)
            list.Add(new ExecutiveAlertVm("info", "Hay saldo pendiente en cartera (facturas con balance abierto)."));

        var noSchedule = await _db.Doctors.AsNoTracking()
            .CountAsync(d => !d.IsDeleted && d.IsActive && (d.WorkingHours == null || d.WorkingHours.Trim() == ""), cancellationToken);
        if (noSchedule > 0)
            list.Add(new ExecutiveAlertVm("secondary", $"{noSchedule} doctor(es) sin horario configurado (campo horario vacío)."));

        var failedEvents = await _db.EventLogs.AsNoTracking()
            .CountAsync(e => !e.IsDeleted && e.Status == OutboxEventStatus.Failed, cancellationToken);
        if (failedEvents > 0)
            list.Add(new ExecutiveAlertVm("warning", $"{failedEvents} evento(s) de integración en estado fallido (outbox)."));

        if (list.Count == 0)
            list.Add(new ExecutiveAlertVm("success", "Sin alertas críticas en este momento."));

        return list;
    }

    private async Task<List<ActivityItemVm>> BuildRecentActivityAsync(CancellationToken cancellationToken)
    {
        var merged = new List<(DateTime Utc, ActivityItemVm Item)>();

        var audits = await _db.AuditLogs.AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(6)
            .Select(a => new { a.CreatedAt, a.Action, a.Module, a.Description })
            .ToListAsync(cancellationToken);
        foreach (var a in audits)
        {
            merged.Add((a.CreatedAt, new ActivityItemVm(
                "fa fa-shield text-info",
                $"{a.Action} · {a.Module}" + (a.Description != null ? $" — {a.Description}" : ""),
                a.CreatedAt.ToLocalTime().ToString("g"),
                null,
                a.CreatedAt)));
        }

        var payments = await _db.Payments.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.PaymentDate)
            .Take(4)
            .Select(p => new { p.PaymentDate, p.Amount, p.Patient!.NombreCompleto })
            .ToListAsync(cancellationToken);
        foreach (var p in payments)
        {
            merged.Add((p.PaymentDate, new ActivityItemVm(
                "fa fa-money text-success",
                $"Pago {p.Amount:N2} · {p.NombreCompleto}",
                p.PaymentDate.ToLocalTime().ToString("g"),
                null,
                p.PaymentDate)));
        }

        var apts = await _db.Appointments.AsNoTracking()
            .Where(a => !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .Take(4)
            .Select(a => new { a.CreatedAt, a.Status, a.Patient!.NombreCompleto })
            .ToListAsync(cancellationToken);
        foreach (var a in apts)
        {
            merged.Add((a.CreatedAt, new ActivityItemVm(
                "fa fa-calendar text-primary",
                $"Cita {a.Status} · {a.NombreCompleto}",
                a.CreatedAt.ToLocalTime().ToString("g"),
                null,
                a.CreatedAt)));
        }

        return merged.OrderByDescending(x => x.Utc).Take(14).Select(x => x.Item).ToList();
    }

    public async Task<AppointmentsReportVm> GetAppointmentsReportAsync(AppointmentsReportFilter filter, CancellationToken cancellationToken = default)
    {
        var from = filter.From?.Date ?? DateTime.Today.AddDays(-30);
        var to = filter.To?.Date ?? DateTime.Today;
        var toEnd = to.AddDays(1);

        var q = _db.Appointments.AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => !a.IsDeleted && a.ScheduledDate >= from && a.ScheduledDate < toEnd);

        if (filter.DoctorId.HasValue)
            q = q.Where(a => a.DoctorId == filter.DoctorId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Speciality))
            q = q.Where(a => a.Doctor != null && a.Doctor.Speciality == filter.Speciality);
        if (filter.Status.HasValue)
            q = q.Where(a => (int)a.Status == filter.Status.Value);

        var rows = await q
            .OrderByDescending(a => a.ScheduledDate).ThenBy(a => a.StartTime)
            .Select(a => new AppointmentsReportRowVm(
                a.Id,
                a.ScheduledDate,
                a.StartTime,
                a.Patient!.NombreCompleto,
                a.Doctor!.FirstName + " " + a.Doctor.LastName,
                a.Doctor.Speciality ?? "—",
                a.Status.ToString(),
                a.Reason))
            .ToListAsync(cancellationToken);

        var totalsRaw = await q
            .GroupBy(a => a.Status)
            .Select(g => new { Name = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(cancellationToken);
        var totals = totalsRaw
            .Select(x => new NamedCountVm(x.Name, x.Count))
            .ToList();

        return new AppointmentsReportVm(rows, totals, rows.Count);
    }

    public async Task<PatientsReportVm> GetPatientsReportAsync(PatientsReportFilter filter, CancellationToken cancellationToken = default)
    {
        var from = filter.From?.Date ?? DateTime.Today.AddDays(-90);
        var to = filter.To?.Date ?? DateTime.Today;
        var toEnd = to.AddDays(1);
        var fromUtc = DateTime.SpecifyKind(from, DateTimeKind.Utc);
        var toEndUtc = DateTime.SpecifyKind(toEnd, DateTimeKind.Utc);

        var q = _db.Patients.AsNoTracking().Where(p => !p.IsDeleted);
        if (!filter.IncludeInactive)
            q = q.Where(p => p.IsActive);

        // Proyectar a anónimo para que EF pueda traducir el OrderByDescending sobre AppointmentCount
        var rawRows = await q
            .Select(p => new
            {
                p.Id,
                p.NombreCompleto,
                p.CreatedAt,
                p.IsActive,
                AppointmentCount = _db.Appointments.Count(a => a.PatientId == p.Id && !a.IsDeleted)
            })
            .OrderByDescending(p => p.AppointmentCount)
            .ToListAsync(cancellationToken);

        var rows = rawRows
            .Select(p => new PatientsReportRowVm(p.Id, p.NombreCompleto, p.CreatedAt, p.IsActive, p.AppointmentCount))
            .ToList();

        var newInPeriod = await _db.Patients.AsNoTracking()
            .CountAsync(p => !p.IsDeleted && p.CreatedAt >= fromUtc && p.CreatedAt < toEndUtc, cancellationToken);

        var top = rows.Take(15).Select(p => new NamedCountVm(p.Name, p.AppointmentCount)).ToList();

        return new PatientsReportVm(rows, newInPeriod, top);
    }

    public async Task<FinancialReportVm> GetFinancialReportAsync(FinancialReportFilter filter, CancellationToken cancellationToken = default)
    {
        var from = filter.From?.Date ?? DateTime.Today.AddDays(-30);
        var to = filter.To?.Date ?? DateTime.Today;
        var toEnd = DateTime.SpecifyKind(to.AddDays(1), DateTimeKind.Utc);
        var fromUtc = DateTime.SpecifyKind(from, DateTimeKind.Utc);

        var invQ = _db.BillingInvoices.AsNoTracking()
            .Include(i => i.Patient)
            .Where(i => !i.IsDeleted && i.IssueDate >= fromUtc && i.IssueDate < toEnd);
        if (filter.PatientId.HasValue)
            invQ = invQ.Where(i => i.PatientId == filter.PatientId.Value);

        var invoices = await invQ
            .OrderByDescending(i => i.IssueDate)
            .Select(i => new InvoiceReportRowVm(
                i.Id,
                i.InvoiceNumber,
                i.IssueDate,
                i.Patient!.NombreCompleto,
                i.TotalAmount,
                i.BalanceDue,
                i.Status.ToString()))
            .ToListAsync(cancellationToken);

        var payQ = _db.Payments.AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.BillingInvoice)
            .Where(p => !p.IsDeleted && p.Status == PaymentEntryStatus.Registered &&
                p.PaymentDate >= fromUtc && p.PaymentDate < toEnd);
        if (filter.PatientId.HasValue)
            payQ = payQ.Where(p => p.PatientId == filter.PatientId.Value);
        if (filter.PaymentMethod.HasValue)
            payQ = payQ.Where(p => (int)p.PaymentMethod == filter.PaymentMethod.Value);

        var payments = await payQ
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new PaymentReportRowVm(
                p.Id,
                p.PaymentDate,
                p.Patient!.NombreCompleto,
                p.BillingInvoice!.InvoiceNumber,
                p.Amount,
                p.PaymentMethod.ToString()))
            .ToListAsync(cancellationToken);

        var totalInv = await invQ.Where(i => i.Status != InvoiceStatus.Cancelled).SumAsync(i => i.TotalAmount, cancellationToken);
        var collected = await payQ.SumAsync(p => p.Amount, cancellationToken);
        var outstanding = await _db.BillingInvoices.AsNoTracking()
            .Where(i => !i.IsDeleted && i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => i.BalanceDue, cancellationToken);

        var byMethod = await payQ
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new NamedDecimalVm(g.Key.ToString(), g.Sum(x => x.Amount)))
            .ToListAsync(cancellationToken);

        var summary = new FinancialSummaryVm(totalInv, collected, outstanding, invoices.Count);

        return new FinancialReportVm(summary, invoices, payments, byMethod);
    }

    public async Task<DoctorsReportVm> GetDoctorsReportAsync(DoctorsReportFilter filter, CancellationToken cancellationToken = default)
    {
        var from = filter.From?.Date ?? DateTime.Today.AddDays(-30);
        var to = filter.To?.Date ?? DateTime.Today;
        var toEnd = to.AddDays(1);

        var q = _db.Appointments.AsNoTracking()
            .Where(a => !a.IsDeleted && a.ScheduledDate >= from && a.ScheduledDate < toEnd);
        if (filter.DoctorId.HasValue)
            q = q.Where(a => a.DoctorId == filter.DoctorId.Value);

        // Proyectar a anónimo para que EF pueda traducir el OrderByDescending sobre TotalAppointments
        var rawGrouped = await q
            .GroupBy(a => new { a.DoctorId, a.Doctor!.FirstName, a.Doctor.LastName, a.Doctor.Speciality })
            .Select(g => new
            {
                g.Key.DoctorId,
                DoctorName = g.Key.FirstName + " " + g.Key.LastName,
                g.Key.Speciality,
                TotalAppointments = g.Count(),
                Completed = g.Count(x => x.Status == AppointmentStatus.Completed),
                Cancelled = g.Count(x => x.Status == AppointmentStatus.Cancelled),
                NoShow = g.Count(x => x.Status == AppointmentStatus.NoShow)
            })
            .OrderByDescending(r => r.TotalAppointments)
            .ToListAsync(cancellationToken);

        var grouped = rawGrouped
            .Select(r => new DoctorsReportRowVm(r.DoctorId, r.DoctorName, r.Speciality, r.TotalAppointments, r.Completed, r.Cancelled, r.NoShow))
            .ToList();

        var avg = grouped.Count == 0 ? 0 : Math.Round((decimal)grouped.Sum(x => x.TotalAppointments) / grouped.Count, 1);

        return new DoctorsReportVm(grouped, avg);
    }
}
