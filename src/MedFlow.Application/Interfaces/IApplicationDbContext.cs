using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace MedFlow.Application.Interfaces;

public interface IApplicationDbContext
{
    DatabaseFacade Database { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<TenantSettings> TenantSettings { get; }
    DbSet<Patient> Patients { get; }
    DbSet<Doctor> Doctors { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<MedicalRecord> MedicalRecords { get; }
    DbSet<Prescription> Prescriptions { get; }
    DbSet<MedicalAttachment> MedicalAttachments { get; }
    DbSet<BillingInvoice> BillingInvoices { get; }
    DbSet<BillingInvoiceItem> BillingInvoiceItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<CashMovement> CashMovements { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<EventLog> EventLogs { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<TenantSubscription> TenantSubscriptions { get; }
    DbSet<TenantSubscriptionHistory> TenantSubscriptionHistories { get; }
    DbSet<TenantBillingProfile> TenantBillingProfiles { get; }
    DbSet<SaaSBillingTransaction> SaaSBillingTransactions { get; }
    DbSet<SaaSInvoice> SaaSInvoices { get; }
    DbSet<StripeWebhookEventLog> StripeWebhookEventLogs { get; }
    DbSet<NotificationTemplate> NotificationTemplates { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }
    DbSet<NotificationMessage> NotificationMessages { get; }
    DbSet<WorkflowDefinition> WorkflowDefinitions { get; }
    DbSet<WorkflowExecution> WorkflowExecutions { get; }
    DbSet<AIInsight> AIInsights { get; }
    DbSet<TenantDailySnapshot> TenantDailySnapshots { get; }
    DbSet<AnalyticsJobLog> AnalyticsJobLogs { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<PushDeviceToken> PushDeviceTokens { get; }
    DbSet<WorkerHeartbeat> WorkerHeartbeats { get; }

    // Contabilidad
    DbSet<Account> Accounts { get; }
    DbSet<FiscalPeriod> FiscalPeriods { get; }
    DbSet<JournalEntry> JournalEntries { get; }
    DbSet<JournalEntryLine> JournalEntryLines { get; }
    DbSet<TaxRate> TaxRates { get; }
    DbSet<BankAccount> BankAccounts { get; }
    DbSet<BankTransaction> BankTransactions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
