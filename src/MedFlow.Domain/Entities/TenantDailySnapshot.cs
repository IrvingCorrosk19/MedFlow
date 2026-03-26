namespace MedFlow.Domain.Entities;

/// <summary>
/// Snapshot diario de métricas operativas por tenant. Base para analítica histórica, tendencias y benchmarking.
/// </summary>
public class TenantDailySnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public DateTime SnapshotDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public int ActiveUsersCount { get; set; }
    public int ActiveDoctorsCount { get; set; }
    public int ActivePatientsCount { get; set; }
    public int NewPatientsCount { get; set; }

    public int AppointmentsCreatedCount { get; set; }
    public int AppointmentsConfirmedCount { get; set; }
    public int AppointmentsScheduled { get; set; }
    public int AppointmentsCompleted { get; set; }
    public int AppointmentsCancelled { get; set; }
    public int AppointmentsNoShow { get; set; }
    public int AppointmentsRescheduled { get; set; }
    public int AppointmentsTotal { get; set; }

    public int PatientsTotal { get; set; }
    public int PatientsNewInPeriod { get; set; }
    public int DoctorsActive { get; set; }
    public int MedicalRecordsCreated { get; set; }

    public int InvoicesCreated { get; set; }
    public int InvoicesPaid { get; set; }
    public int InvoicesOverdue { get; set; }
    public int InvoicesPendingCount { get; set; }
    public decimal TotalInvoicedAmount { get; set; }
    public decimal RevenueCollected { get; set; }
    public decimal BalanceDueTotal { get; set; }
    public int PaymentsCount { get; set; }

    public int NotificationsSentCount { get; set; }
    public int NotificationsFailedCount { get; set; }

    public int WorkflowExecutionsTotal { get; set; }
    public int WorkflowExecutionsSuccess { get; set; }
    public int WorkflowExecutionsFailed { get; set; }
    public int AIInsightsGenerated { get; set; }
    public int AIInsightsCritical { get; set; }
    public int AIInsightsNew { get; set; }

    public string? SubscriptionStatus { get; set; }
    public string? PlanCode { get; set; }

    public int AppointmentsCompletedLast7 { get; set; }
    public int AppointmentsCompletedLast30 { get; set; }
    public decimal RevenueLast7 { get; set; }
    public decimal RevenueLast30 { get; set; }
}
