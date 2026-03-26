using MedFlow.Application.Interfaces;
using MedFlow.Domain.Common;
using MedFlow.Domain.Entities;
using MedFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<
    ApplicationUser,
    ApplicationRole,
    string,
    IdentityUserClaim<string>,
    ApplicationUserRole,
    IdentityUserLogin<string>,
    IdentityRoleClaim<string>,
    IdentityUserToken<string>>, IApplicationDbContext
{
    private readonly ITenantContext _tenant;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<MedicalAttachment> MedicalAttachments => Set<MedicalAttachment>();
    public DbSet<BillingInvoice> BillingInvoices => Set<BillingInvoice>();
    public DbSet<BillingInvoiceItem> BillingInvoiceItems => Set<BillingInvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<EventLog> EventLogs => Set<EventLog>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<TenantSubscriptionHistory> TenantSubscriptionHistories => Set<TenantSubscriptionHistory>();
    public DbSet<TenantBillingProfile> TenantBillingProfiles => Set<TenantBillingProfile>();
    public DbSet<SaaSBillingTransaction> SaaSBillingTransactions => Set<SaaSBillingTransaction>();
    public DbSet<SaaSInvoice> SaaSInvoices => Set<SaaSInvoice>();
    public DbSet<StripeWebhookEventLog> StripeWebhookEventLogs => Set<StripeWebhookEventLog>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<NotificationMessage> NotificationMessages => Set<NotificationMessage>();
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowExecution> WorkflowExecutions => Set<WorkflowExecution>();
    public DbSet<AIInsight> AIInsights => Set<AIInsight>();
    public DbSet<TenantDailySnapshot> TenantDailySnapshots => Set<TenantDailySnapshot>();
    public DbSet<AnalyticsJobLog> AnalyticsJobLogs => Set<AnalyticsJobLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PushDeviceToken> PushDeviceTokens => Set<PushDeviceToken>();
    public DbSet<WorkerHeartbeat> WorkerHeartbeats => Set<WorkerHeartbeat>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.Entity<Tenant>()
            .HasQueryFilter(t => !t.IsDeleted);

        builder.Entity<Patient>().HasQueryFilter(p =>
            !p.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && p.TenantId == _tenant.TenantId)));

        builder.Entity<Doctor>().HasQueryFilter(d =>
            !d.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && d.TenantId == _tenant.TenantId)));

        builder.Entity<Appointment>().HasQueryFilter(a =>
            !a.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && a.TenantId == _tenant.TenantId)));

        builder.Entity<MedicalRecord>().HasQueryFilter(m =>
            !m.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && m.TenantId == _tenant.TenantId)));

        builder.Entity<BillingInvoice>().HasQueryFilter(i =>
            !i.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && i.TenantId == _tenant.TenantId)));

        builder.Entity<BillingInvoiceItem>().HasQueryFilter(l =>
            !l.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && l.TenantId == _tenant.TenantId)));

        builder.Entity<Payment>().HasQueryFilter(p =>
            !p.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && p.TenantId == _tenant.TenantId)));

        builder.Entity<CashMovement>().HasQueryFilter(c =>
            !c.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && c.TenantId == _tenant.TenantId)));

        builder.Entity<Notification>().HasQueryFilter(n =>
            !n.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && n.TenantId == _tenant.TenantId)));

        builder.Entity<EventLog>().HasQueryFilter(e =>
            !e.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && e.TenantId == _tenant.TenantId)));

        builder.Entity<Permission>().HasQueryFilter(p => !p.IsDeleted);

        builder.Entity<Prescription>().HasQueryFilter(x =>
            !x.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && x.TenantId == _tenant.TenantId)));

        builder.Entity<MedicalAttachment>().HasQueryFilter(x =>
            !x.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && x.TenantId == _tenant.TenantId)));

        builder.Entity<TenantSettings>().HasQueryFilter(s =>
            _tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && s.TenantId == _tenant.TenantId));

        builder.Entity<AuditLog>().HasQueryFilter(a =>
            _tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && a.TenantId == _tenant.TenantId));

        builder.Entity<TenantSubscription>().HasQueryFilter(ts =>
            !ts.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && ts.TenantId == _tenant.TenantId)));

        builder.Entity<TenantSubscriptionHistory>().HasQueryFilter(h =>
            !h.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && h.TenantId == _tenant.TenantId)));

        builder.Entity<TenantBillingProfile>().HasQueryFilter(b =>
            !b.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && b.TenantId == _tenant.TenantId)));

        builder.Entity<SaaSBillingTransaction>().HasQueryFilter(t =>
            !t.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && t.TenantId == _tenant.TenantId)));

        builder.Entity<SaaSInvoice>().HasQueryFilter(i =>
            !i.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && i.TenantId == _tenant.TenantId)));

        builder.Entity<StripeWebhookEventLog>().HasQueryFilter(e => !e.IsDeleted);

        builder.Entity<NotificationTemplate>().HasQueryFilter(t =>
            !t.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && t.TenantId == _tenant.TenantId)));

        builder.Entity<NotificationPreference>().HasQueryFilter(p =>
            !p.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && p.TenantId == _tenant.TenantId)));

        builder.Entity<NotificationMessage>().HasQueryFilter(m =>
            !m.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && m.TenantId == _tenant.TenantId)));

        builder.Entity<WorkflowDefinition>().HasQueryFilter(w =>
            !w.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && w.TenantId == _tenant.TenantId)));

        builder.Entity<WorkflowExecution>().HasQueryFilter(e =>
            !e.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && e.TenantId == _tenant.TenantId)));

        builder.Entity<AIInsight>().HasQueryFilter(i =>
            !i.IsDeleted && (_tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && i.TenantId == _tenant.TenantId)));

        builder.Entity<ApplicationUser>().HasQueryFilter(u =>
            _tenant.IgnoreTenantFilter || (_tenant.TenantId.HasValue && u.TenantId == _tenant.TenantId));
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var tid = _tenant.TenantId;
        if (tid.HasValue)
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State != EntityState.Added)
                    continue;
                if (entry.Entity is ITenantScopedEntity scoped && scoped.TenantId == Guid.Empty)
                    scoped.TenantId = tid.Value;
            }
        }

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        foreach (var entry in ChangeTracker.Entries<ApplicationUser>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        foreach (var entry in ChangeTracker.Entries<ApplicationRole>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
