using MedFlow.Application.Interfaces;
using MedFlow.Application.Saas;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public sealed class SaasTenantAdminService : ISaasTenantAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly ISubscriptionLimitService _limits;

    public SaasTenantAdminService(ApplicationDbContext db, ISubscriptionLimitService limits)
    {
        _db = db;
        _limits = limits;
    }

    public async Task<IReadOnlyList<SaasTenantListItemDto>> GetTenantsAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _db.Tenants
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        var list = new List<SaasTenantListItemDto>();
        foreach (var t in tenants)
        {
            var sub = await GetOperationalSubscriptionAsync(t.Id, cancellationToken);
            list.Add(new SaasTenantListItemDto
            {
                Id = t.Id,
                Name = t.Name,
                Code = t.Code,
                CommercialStatus = t.CommercialStatus,
                IsSuspended = t.IsSuspended,
                PlanName = sub?.SubscriptionPlan?.Name,
                SubscriptionStatus = sub?.Status,
                TrialEndDate = sub?.TrialEndDate,
                EndDate = sub?.EndDate
            });
        }

        return list;
    }

    public async Task<SaasTenantDetailsDto?> GetTenantDetailsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var t = await _db.Tenants
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tenantId && !x.IsDeleted, cancellationToken);
        if (t == null) return null;

        var sub = await GetOperationalSubscriptionAsync(tenantId, cancellationToken);
        var usage = await _limits.GetCurrentUsageAsync(tenantId, cancellationToken);

        var history = await _db.TenantSubscriptionHistories
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(h => h.TenantId == tenantId)
            .Include(h => h.PreviousPlan)
            .Include(h => h.NewPlan)
            .OrderByDescending(h => h.CreatedAt)
            .Take(50)
            .Select(h => new SaasSubscriptionHistoryItemDto
            {
                Id = h.Id,
                PreviousPlanName = h.PreviousPlan != null ? h.PreviousPlan.Name : null,
                NewPlanName = h.NewPlan.Name,
                PreviousStatus = h.PreviousStatus,
                NewStatus = h.NewStatus,
                ChangeReason = h.ChangeReason,
                CreatedAt = h.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new SaasTenantDetailsDto
        {
            Id = t.Id,
            Name = t.Name,
            Code = t.Code,
            Email = t.Email,
            CommercialStatus = t.CommercialStatus,
            IsSuspended = t.IsSuspended,
            SuspensionReason = t.SuspensionReason,
            ActivatedAt = t.ActivatedAt,
            SuspendedAt = t.SuspendedAt,
            TenantSubscriptionId = sub?.Id,
            SubscriptionPlanId = sub?.SubscriptionPlanId,
            PlanName = sub?.SubscriptionPlan?.Name,
            PlanCode = sub?.SubscriptionPlan?.Code,
            SubscriptionStatus = sub?.Status,
            TrialEndDate = sub?.TrialEndDate,
            EndDate = sub?.EndDate,
            NextBillingDate = sub?.NextBillingDate,
            Usage = usage,
            History = history
        };
    }

    public async Task<Guid> CreateTenantWithSubscriptionAsync(SaasTenantCreateDto dto, string? changedByUserId, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(dto.Code);
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("El código de clínica es obligatorio.");

        var exists = await _db.Tenants.IgnoreQueryFilters()
            .AnyAsync(t => t.Code == code && !t.IsDeleted, cancellationToken);
        if (exists)
            throw new InvalidOperationException("Ya existe una clínica con ese código.");

        var plan = await _db.SubscriptionPlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == dto.SubscriptionPlanId && !p.IsDeleted, cancellationToken);
        if (plan == null)
            throw new InvalidOperationException("Plan no encontrado.");

        // NpgsqlRetryingExecutionStrategy no soporta transacciones "user-initiated" fuera de un execution strategy.
        // Por eso envolvemos el flujo completo (incluida la transacción) para permitir reintentos seguros.
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            var tenant = new Tenant
            {
                Name = dto.Name.Trim(),
                Code = code,
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
                CommercialStatus = dto.StartWithTrial ? TenantCommercialStatus.Trial : TenantCommercialStatus.Active,
                ActivatedAt = DateTime.UtcNow,
                IsSuspended = false
            };
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync(cancellationToken);

            var now = DateTime.UtcNow;
            var sub = new TenantSubscription
            {
                TenantId = tenant.Id,
                SubscriptionPlanId = plan.Id,
                StartDate = now,
                Status = dto.StartWithTrial && plan.TrialDays > 0 ? SubscriptionStatus.Trial : SubscriptionStatus.Active,
                TrialStartDate = dto.StartWithTrial && plan.TrialDays > 0 ? now : null,
                TrialEndDate = dto.StartWithTrial && plan.TrialDays > 0 ? now.AddDays(plan.TrialDays) : null,
                EndDate = null,
                NextBillingDate = now.AddMonths(1)
            };

            _db.TenantSubscriptions.Add(sub);
            await _db.SaveChangesAsync(cancellationToken);

            tenant.CurrentSubscriptionId = sub.Id;
            tenant.CommercialStatus = sub.Status == SubscriptionStatus.Trial
                ? TenantCommercialStatus.Trial
                : TenantCommercialStatus.Active;
            _db.Tenants.Update(tenant);
            await _db.SaveChangesAsync(cancellationToken);

            _db.TenantSubscriptionHistories.Add(new TenantSubscriptionHistory
            {
                TenantId = tenant.Id,
                PreviousPlanId = null,
                NewPlanId = plan.Id,
                PreviousStatus = null,
                NewStatus = sub.Status,
                ChangeReason = "Alta de clínica y asignación de plan inicial.",
                ChangedByUserId = changedByUserId
            });
            await _db.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);
            return tenant.Id;
        });
    }

    public async Task SuspendTenantAsync(Guid tenantId, string reason, string? changedByUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Indique el motivo de la suspensión.");

        var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);
        if (tenant == null) throw new InvalidOperationException("Clínica no encontrada.");

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            var sub = await GetTrackedOperationalSubscriptionAsync(tenantId, cancellationToken);
            var prev = sub?.Status ?? SubscriptionStatus.Active;

            tenant.IsSuspended = true;
            tenant.SuspensionReason = reason.Trim();
            tenant.SuspendedAt = DateTime.UtcNow;
            tenant.CommercialStatus = TenantCommercialStatus.Suspended;

            if (sub != null)
            {
                sub.Status = SubscriptionStatus.Suspended;
                sub.SuspendedAt = DateTime.UtcNow;
                sub.UpdatedAt = DateTime.UtcNow;

                _db.TenantSubscriptionHistories.Add(new TenantSubscriptionHistory
                {
                    TenantId = tenantId,
                    PreviousPlanId = sub.SubscriptionPlanId,
                    NewPlanId = sub.SubscriptionPlanId,
                    PreviousStatus = prev,
                    NewStatus = SubscriptionStatus.Suspended,
                    ChangeReason = "Suspensión comercial: " + reason.Trim(),
                    ChangedByUserId = changedByUserId
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        });
    }

    public async Task ActivateTenantAsync(Guid tenantId, string? changedByUserId, CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);
        if (tenant == null) throw new InvalidOperationException("Clínica no encontrada.");

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            var sub = await _db.TenantSubscriptions
                .IgnoreQueryFilters()
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.TenantId == tenantId && !s.IsDeleted)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync(cancellationToken);

            tenant.IsSuspended = false;
            tenant.SuspensionReason = null;
            tenant.SuspendedAt = null;
            tenant.ActivatedAt = DateTime.UtcNow;

            if (sub != null && sub.Status == SubscriptionStatus.Suspended)
            {
                var now = DateTime.UtcNow;
                SubscriptionStatus newStatus;
                if (sub.TrialEndDate.HasValue && now <= sub.TrialEndDate.Value)
                {
                    newStatus = SubscriptionStatus.Trial;
                    sub.Status = newStatus;
                    tenant.CommercialStatus = TenantCommercialStatus.Trial;
                }
                else
                {
                    newStatus = SubscriptionStatus.Active;
                    sub.Status = newStatus;
                    sub.SuspendedAt = null;
                    tenant.CommercialStatus = TenantCommercialStatus.Active;
                }

                sub.UpdatedAt = DateTime.UtcNow;

                _db.TenantSubscriptionHistories.Add(new TenantSubscriptionHistory
                {
                    TenantId = tenantId,
                    PreviousPlanId = sub.SubscriptionPlanId,
                    NewPlanId = sub.SubscriptionPlanId,
                    PreviousStatus = SubscriptionStatus.Suspended,
                    NewStatus = newStatus,
                    ChangeReason = "Reactivación de clínica.",
                    ChangedByUserId = changedByUserId
                });
            }
            else
            {
                tenant.CommercialStatus = TenantCommercialStatus.Active;
            }

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        });
    }

    public async Task ChangePlanAsync(Guid tenantId, Guid newPlanId, string reason, string? changedByUserId, CancellationToken cancellationToken = default)
    {
        var newPlan = await _db.SubscriptionPlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == newPlanId && !p.IsDeleted, cancellationToken);
        if (newPlan == null) throw new InvalidOperationException("Plan destino no encontrado.");

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            // Mantener coherencia: si el tenant está suspendido, el cambio de plan no debería “reactivar” comercialmente
            // ni dejar una suscripción Active mientras el tenant sigue IsSuspended=true.
            var tenant = await _db.Tenants.IgnoreQueryFilters()
                .FirstAsync(t => t.Id == tenantId, cancellationToken);
            var keepSuspended = tenant.IsSuspended;

            var oldSub = await _db.TenantSubscriptions
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == tenantId && !s.IsDeleted)
                .Where(s => s.Status == SubscriptionStatus.Trial
                            || s.Status == SubscriptionStatus.Active
                            || s.Status == SubscriptionStatus.PastDue
                            || s.Status == SubscriptionStatus.Suspended)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync(cancellationToken);

            Guid? prevPlanId = oldSub?.SubscriptionPlanId;
            SubscriptionStatus? prevStatus = oldSub?.Status;

            if (oldSub != null)
            {
                oldSub.Status = SubscriptionStatus.Cancelled;
                oldSub.CancelledAt = DateTime.UtcNow;
                oldSub.EndDate = DateTime.UtcNow;
                oldSub.UpdatedAt = DateTime.UtcNow;
            }

            var now = DateTime.UtcNow;
            var newSubStatus = keepSuspended ? SubscriptionStatus.Suspended : SubscriptionStatus.Active;
            var newSub = new TenantSubscription
            {
                TenantId = tenantId,
                SubscriptionPlanId = newPlanId,
                StartDate = now,
                Status = newSubStatus,
                SuspendedAt = keepSuspended ? DateTime.UtcNow : null,
                NextBillingDate = now.AddMonths(1)
            };
            _db.TenantSubscriptions.Add(newSub);
            await _db.SaveChangesAsync(cancellationToken);

            tenant.CurrentSubscriptionId = newSub.Id;
            tenant.CommercialStatus = keepSuspended
                ? TenantCommercialStatus.Suspended
                : TenantCommercialStatus.Active;
            await _db.SaveChangesAsync(cancellationToken);

            _db.TenantSubscriptionHistories.Add(new TenantSubscriptionHistory
            {
                TenantId = tenantId,
                PreviousPlanId = prevPlanId,
                NewPlanId = newPlanId,
                PreviousStatus = prevStatus,
                NewStatus = newSub.Status,
                ChangeReason = string.IsNullOrWhiteSpace(reason) ? "Cambio de plan." : reason.Trim(),
                ChangedByUserId = changedByUserId
            });
            await _db.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);
        });
    }

    private async Task<TenantSubscription?> GetOperationalSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _db.TenantSubscriptions
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.TenantId == tenantId && !s.IsDeleted)
            .Where(s => s.Status == SubscriptionStatus.Trial || s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.PastDue || s.Status == SubscriptionStatus.Suspended)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private Task<TenantSubscription?> GetTrackedOperationalSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken) =>
        _db.TenantSubscriptions
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId && !s.IsDeleted)
            .Where(s => s.Status == SubscriptionStatus.Trial || s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.PastDue)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

    private static string NormalizeCode(string code) =>
        code.Trim().ToLowerInvariant().Replace(' ', '-');
}
