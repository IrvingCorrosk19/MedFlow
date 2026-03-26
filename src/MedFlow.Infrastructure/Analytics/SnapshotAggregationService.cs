using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Analytics;

public sealed class SnapshotAggregationService : ISnapshotAggregationService, IAnalyticsAggregationService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public SnapshotAggregationService(ApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task AggregateDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        _tenant.SetIgnoreTenantFilter(true);
        try
        {
            var tenantIds = await _db.Tenants
                .Where(t => !t.IsDeleted && !t.IsSuspended)
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);

            foreach (var tenantId in tenantIds)
            {
                try
                {
                    await AggregateTenantForDateAsync(tenantId, date, cancellationToken);
                }
                catch (Exception)
                {
                    // Log but continue with other tenants
                }
            }
        }
        finally
        {
            _tenant.SetIgnoreTenantFilter(false);
        }
    }

    public async Task AggregateTenantForDateAsync(Guid tenantId, DateTime date, CancellationToken cancellationToken = default)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);
        var dayMinus7 = dayStart.AddDays(-7);
        var dayMinus30 = dayStart.AddDays(-30);

        var existing = await _db.TenantDailySnapshots
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.SnapshotDate == dayStart, cancellationToken);
        if (existing != null)
            _db.TenantDailySnapshots.Remove(existing);

        var aptByStatus = await _db.Appointments
            .Where(a => a.TenantId == tenantId && a.ScheduledDate >= dayStart && a.ScheduledDate < dayEnd && !a.IsDeleted)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int GetApt(AppointmentStatus s) => aptByStatus.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

        var aptTotal = aptByStatus.Sum(x => x.Count);
        var aptCompleted = GetApt(AppointmentStatus.Completed);
        var aptCancelled = GetApt(AppointmentStatus.Cancelled);
        var aptNoShow = GetApt(AppointmentStatus.NoShow);
        var aptRescheduled = GetApt(AppointmentStatus.Rescheduled);
        var aptScheduled = GetApt(AppointmentStatus.Scheduled) + GetApt(AppointmentStatus.Confirmed) + GetApt(AppointmentStatus.InProgress);

        var patientsTotal = await _db.Patients.CountAsync(p => p.TenantId == tenantId && !p.IsDeleted, cancellationToken);
        var patientsNew = await _db.Patients.CountAsync(p => p.TenantId == tenantId && !p.IsDeleted && p.CreatedAt >= dayStart && p.CreatedAt < dayEnd, cancellationToken);
        var doctorsActive = await _db.Doctors.CountAsync(d => d.TenantId == tenantId && !d.IsDeleted, cancellationToken);
        var medicalRecords = await _db.MedicalRecords.CountAsync(m => m.TenantId == tenantId && !m.IsDeleted && m.VisitDate >= dayStart && m.VisitDate < dayEnd, cancellationToken);

        var invoicesCreated = await _db.BillingInvoices.CountAsync(i => i.TenantId == tenantId && !i.IsDeleted && i.IssueDate >= dayStart && i.IssueDate < dayEnd, cancellationToken);
        var invoicesPaid = await _db.BillingInvoices.CountAsync(i => i.TenantId == tenantId && !i.IsDeleted && i.Status == InvoiceStatus.Paid && i.IssueDate >= dayStart && i.IssueDate < dayEnd, cancellationToken);
        var invoicesOverdue = await _db.BillingInvoices.CountAsync(i => i.TenantId == tenantId && !i.IsDeleted && i.BalanceDue > 0 && i.DueDate < dayEnd, cancellationToken);
        var revenueCollected = await _db.Payments
            .Where(p => p.TenantId == tenantId && !p.IsDeleted && p.PaymentDate >= dayStart && p.PaymentDate < dayEnd)
            .SumAsync(p => p.Amount, cancellationToken);
        var balanceDue = await _db.BillingInvoices
            .Where(i => i.TenantId == tenantId && !i.IsDeleted && i.BalanceDue > 0)
            .SumAsync(i => i.BalanceDue, cancellationToken);
        var paymentsCount = await _db.Payments.CountAsync(p => p.TenantId == tenantId && !p.IsDeleted && p.PaymentDate >= dayStart && p.PaymentDate < dayEnd, cancellationToken);

        var aiGenerated = await _db.AIInsights.CountAsync(i => i.TenantId == tenantId && !i.IsDeleted && i.GeneratedAt >= dayStart && i.GeneratedAt < dayEnd, cancellationToken);
        var aiCritical = await _db.AIInsights.CountAsync(i => i.TenantId == tenantId && !i.IsDeleted && i.Severity == AISeverity.Critical && i.GeneratedAt >= dayStart && i.GeneratedAt < dayEnd, cancellationToken);
        var aiNew = await _db.AIInsights.CountAsync(i => i.TenantId == tenantId && !i.IsDeleted && i.Status == AIInsightStatus.New && i.GeneratedAt >= dayStart && i.GeneratedAt < dayEnd, cancellationToken);

        var wfTotal = await _db.WorkflowExecutions.CountAsync(w => w.TenantId == tenantId && !w.IsDeleted && w.StartedAt >= dayStart && w.StartedAt < dayEnd, cancellationToken);
        var wfSuccess = await _db.WorkflowExecutions.CountAsync(w => w.TenantId == tenantId && !w.IsDeleted && w.Status == WorkflowExecutionStatus.Succeeded && w.StartedAt >= dayStart && w.StartedAt < dayEnd, cancellationToken);
        var wfFailed = await _db.WorkflowExecutions.CountAsync(w => w.TenantId == tenantId && !w.IsDeleted && w.Status == WorkflowExecutionStatus.Failed && w.StartedAt >= dayStart && w.StartedAt < dayEnd, cancellationToken);

        var aptCompleted7 = await _db.Appointments.CountAsync(a => a.TenantId == tenantId && !a.IsDeleted && a.Status == AppointmentStatus.Completed && a.ScheduledDate >= dayMinus7 && a.ScheduledDate < dayEnd, cancellationToken);
        var aptCompleted30 = await _db.Appointments.CountAsync(a => a.TenantId == tenantId && !a.IsDeleted && a.Status == AppointmentStatus.Completed && a.ScheduledDate >= dayMinus30 && a.ScheduledDate < dayEnd, cancellationToken);
        var revenue7 = await _db.Payments.Where(p => p.TenantId == tenantId && !p.IsDeleted && p.PaymentDate >= dayMinus7 && p.PaymentDate < dayEnd).SumAsync(p => p.Amount, cancellationToken);
        var revenue30 = await _db.Payments.Where(p => p.TenantId == tenantId && !p.IsDeleted && p.PaymentDate >= dayMinus30 && p.PaymentDate < dayEnd).SumAsync(p => p.Amount, cancellationToken);

        var aptCreated = await _db.Appointments.CountAsync(a => a.TenantId == tenantId && !a.IsDeleted && a.CreatedAt >= dayStart && a.CreatedAt < dayEnd, cancellationToken);
        var aptConfirmed = GetApt(AppointmentStatus.Confirmed);
        var totalInvoiced = await _db.BillingInvoices.Where(i => i.TenantId == tenantId && !i.IsDeleted && i.IssueDate >= dayStart && i.IssueDate < dayEnd).SumAsync(i => i.TotalAmount, cancellationToken);
        var invoicesPending = await _db.BillingInvoices.CountAsync(i => i.TenantId == tenantId && !i.IsDeleted && i.BalanceDue > 0, cancellationToken);

        var notifSent = await _db.NotificationMessages.CountAsync(n => n.TenantId == tenantId && n.Status == NotificationMessageStatus.Sent && n.SentAt >= dayStart && n.SentAt < dayEnd, cancellationToken);
        var notifFailed = await _db.NotificationMessages.CountAsync(n => n.TenantId == tenantId && n.Status == NotificationMessageStatus.Failed && n.CreatedAt >= dayStart && n.CreatedAt < dayEnd, cancellationToken);

        var activeUsers = await _db.Users.CountAsync(u => u.TenantId == tenantId && u.IsActive, cancellationToken);

        var sub = await _db.TenantSubscriptions
            .AsNoTracking()
            .Include(ts => ts.SubscriptionPlan)
            .Where(ts => ts.TenantId == tenantId && !ts.IsDeleted && (ts.Status == SubscriptionStatus.Trial || ts.Status == SubscriptionStatus.Active || ts.Status == SubscriptionStatus.PastDue))
            .OrderByDescending(ts => ts.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        var snapshot = new TenantDailySnapshot
        {
            TenantId = tenantId,
            SnapshotDate = dayStart,
            ActiveUsersCount = activeUsers,
            ActiveDoctorsCount = doctorsActive,
            ActivePatientsCount = patientsTotal,
            NewPatientsCount = patientsNew,
            AppointmentsCreatedCount = aptCreated,
            AppointmentsConfirmedCount = aptConfirmed,
            AppointmentsScheduled = aptScheduled,
            AppointmentsCompleted = aptCompleted,
            AppointmentsCancelled = aptCancelled,
            AppointmentsNoShow = aptNoShow,
            AppointmentsRescheduled = aptRescheduled,
            AppointmentsTotal = aptTotal,
            PatientsTotal = patientsTotal,
            PatientsNewInPeriod = patientsNew,
            DoctorsActive = doctorsActive,
            MedicalRecordsCreated = medicalRecords,
            InvoicesCreated = invoicesCreated,
            InvoicesPaid = invoicesPaid,
            InvoicesOverdue = invoicesOverdue,
            InvoicesPendingCount = invoicesPending,
            TotalInvoicedAmount = totalInvoiced,
            RevenueCollected = revenueCollected,
            BalanceDueTotal = balanceDue,
            PaymentsCount = paymentsCount,
            NotificationsSentCount = notifSent,
            NotificationsFailedCount = notifFailed,
            WorkflowExecutionsTotal = wfTotal,
            WorkflowExecutionsSuccess = wfSuccess,
            WorkflowExecutionsFailed = wfFailed,
            AIInsightsGenerated = aiGenerated,
            AIInsightsCritical = aiCritical,
            AIInsightsNew = aiNew,
            SubscriptionStatus = sub?.Status.ToString(),
            PlanCode = sub?.SubscriptionPlan?.Code,
            AppointmentsCompletedLast7 = aptCompleted7,
            AppointmentsCompletedLast30 = aptCompleted30,
            RevenueLast7 = revenue7,
            RevenueLast30 = revenue30,
            UpdatedAt = DateTime.UtcNow
        };

        _db.TenantDailySnapshots.Add(snapshot);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AggregateTenantForDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var d = from.Date;
        var end = to.Date;
        while (d <= end)
        {
            await AggregateTenantForDateAsync(tenantId, d, cancellationToken);
            d = d.AddDays(1);
        }
    }

    public Task AggregateTodayAsync(CancellationToken cancellationToken = default)
        => AggregateDateAsync(DateTime.UtcNow.Date, cancellationToken);

    public Task AggregateAllTenantsForDateAsync(DateTime date, CancellationToken cancellationToken = default)
        => AggregateDateAsync(date, cancellationToken);
}
