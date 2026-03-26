using System.Text.Json;
using MedFlow.Application.Interfaces;
using MedFlow.Application.Notifications;
using MedFlow.Application.Options;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Stripe;

namespace MedFlow.Infrastructure.Billing;

public sealed class StripeWebhookProcessor : IStripeWebhookProcessor
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly StripeBillingOptions _options;
    private readonly ILogger<StripeWebhookProcessor> _logger;
    private readonly INotificationDispatchService _notifications;

    public StripeWebhookProcessor(
        ApplicationDbContext db,
        ITenantContext tenant,
        IOptions<StripeBillingOptions> options,
        ILogger<StripeWebhookProcessor> logger,
        INotificationDispatchService notifications)
    {
        _db = db;
        _tenant = tenant;
        _options = options.Value;
        _logger = logger;
        _notifications = notifications;
    }

    public async Task ProcessAsync(string jsonPayload, string signatureHeader, CancellationToken cancellationToken = default)
    {
        Event stripeEvent;
        try
        {
            // Si no hay secret configurado, evitamos verificación de firma para permitir webhooks de prueba
            // (por seguridad, el controller solo llama a este procesador bajo condiciones Development).
            if (string.IsNullOrEmpty(_options.WebhookSecret))
            {
                // Sin secret (solo desarrollo): el payload puede traer la api_version de la cuenta Stripe, no la de Stripe.net.
                stripeEvent = EventUtility.ParseEvent(jsonPayload, throwOnApiVersionMismatch: false);
            }
            else
            {
                stripeEvent = EventUtility.ConstructEvent(jsonPayload, signatureHeader, _options.WebhookSecret);
            }
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature validation failed");
            throw;
        }
        catch (Exception ex)
        {
            // Stripe.NET puede lanzar NullReferenceException u otras excepciones con JSON de prueba incompleto.
            // Tratarlas como payload inválido para que el controller responda 400 (no 500) y no se reintente como error de servidor.
            _logger.LogWarning(ex, "Stripe webhook: payload no parseable como evento Stripe");
            throw new StripeException("Invalid or incomplete Stripe event JSON.");
        }

        var prev = _tenant.IgnoreTenantFilter;
        _tenant.SetIgnoreTenantFilter(true);
        try
        {
            var existing = await _db.StripeWebhookEventLogs.IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.ProviderEventId == stripeEvent.Id, cancellationToken);

            if (existing?.IsProcessed == true)
            {
                _logger.LogDebug("Webhook {EventId} already processed, skipping", stripeEvent.Id);
                return;
            }

            StripeWebhookEventLog logEntry;
            if (existing == null)
            {
                logEntry = new StripeWebhookEventLog
                {
                    ProviderEventId = stripeEvent.Id,
                    EventType = stripeEvent.Type,
                    PayloadJson = jsonPayload.Length > 32000 ? jsonPayload[..32000] : jsonPayload,
                    IsProcessed = false
                };

                try
                {
                    await _db.StripeWebhookEventLogs.AddAsync(logEntry, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                {
                    // Otro proceso insertó el mismo evento concurrentemente.
                    // Recargamos el registro y verificamos si ya fue procesado.
                    _db.ChangeTracker.Clear();
                    logEntry = await _db.StripeWebhookEventLogs.IgnoreQueryFilters()
                        .FirstAsync(e => e.ProviderEventId == stripeEvent.Id, cancellationToken);
                    if (logEntry.IsProcessed)
                    {
                        _logger.LogDebug("Webhook {EventId} processed concurrently, skipping", stripeEvent.Id);
                        return;
                    }
                }
            }
            else
            {
                logEntry = existing;
            }

            try
            {
                await ProcessEventAsync(stripeEvent, cancellationToken);
                logEntry.IsProcessed = true;
                logEntry.ProcessedAt = DateTime.UtcNow;
                logEntry.ErrorMessage = null;
            }
            catch (Exception ex)
            {
                logEntry.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error processing Stripe webhook {EventId} {Type}", stripeEvent.Id, stripeEvent.Type);
                throw;
            }
            finally
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        finally
        {
            _tenant.SetIgnoreTenantFilter(prev);
        }
    }

    private async Task ProcessEventAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await HandleCheckoutSessionCompletedAsync(stripeEvent, cancellationToken);
                break;
            case "customer.subscription.created":
            case "customer.subscription.updated":
                await HandleSubscriptionCreatedOrUpdatedAsync(stripeEvent, cancellationToken);
                break;
            case "customer.subscription.deleted":
                await HandleSubscriptionDeletedAsync(stripeEvent, cancellationToken);
                break;
            case "invoice.paid":
                await HandleInvoicePaidAsync(stripeEvent, cancellationToken);
                break;
            case "invoice.payment_failed":
                await HandleInvoicePaymentFailedAsync(stripeEvent, cancellationToken);
                break;
            case "invoice.finalized":
                await HandleInvoiceFinalizedAsync(stripeEvent, cancellationToken);
                break;
            default:
                _logger.LogDebug("Unhandled Stripe event type: {Type}", stripeEvent.Type);
                break;
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session == null) return;

        var tenantId = GetGuidFromMetadata(session.Metadata, "tenant_id");
        var planId = GetGuidFromMetadata(session.Metadata, "plan_id");
        if (!tenantId.HasValue || !planId.HasValue) return;

        var subService = new SubscriptionService();
        var sub = await subService.GetAsync(session.SubscriptionId ?? "", cancellationToken: cancellationToken);
        if (sub == null) return;

        await SyncSubscriptionFromStripeAsync(tenantId.Value, planId.Value, sub, cancellationToken);

        var firstItem = sub.Items?.Data?.FirstOrDefault();
        var amount = firstItem?.Price?.UnitAmount ?? 0L;
        var currency = firstItem?.Price?.Currency ?? "usd";
        await AddTransactionAsync(tenantId.Value, null, SaasTransactionType.SubscriptionCreated, SaasTransactionStatus.Succeeded,
            (decimal)amount / 100m, currency, sub.Id, null, null, "Checkout completado", null, cancellationToken);
    }

    private async Task HandleSubscriptionCreatedOrUpdatedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var sub = stripeEvent.Data.Object as Subscription;
        if (sub == null) return;

        var tenantId = GetGuidFromMetadata(sub.Metadata, "tenant_id");
        var planId = GetGuidFromMetadata(sub.Metadata, "plan_id");
        if (!tenantId.HasValue)
        {
            var localSub = await _db.TenantSubscriptions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == sub.Id && !s.IsDeleted, cancellationToken);
            if (localSub != null)
            {
                tenantId = localSub.TenantId;
                planId = localSub.SubscriptionPlanId;
            }
        }
        if (!tenantId.HasValue || !planId.HasValue) return;

        await SyncSubscriptionFromStripeAsync(tenantId.Value, planId.Value, sub, cancellationToken);
    }

    private async Task HandleSubscriptionDeletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var sub = stripeEvent.Data.Object as Subscription;
        if (sub == null) return;

        var localSub = await _db.TenantSubscriptions.IgnoreQueryFilters()
            .Include(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == sub.Id && !s.IsDeleted, cancellationToken);
        if (localSub == null) return;

        var prevStatus = localSub.Status;
        localSub.Status = SubscriptionStatus.Cancelled;
        localSub.CancelledAt = DateTime.UtcNow;
        localSub.EndDate = DateTime.UtcNow;
        localSub.CancelAtPeriodEnd = false;

        var tenant = await _db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Id == localSub.TenantId, cancellationToken);
        tenant.CommercialStatus = TenantCommercialStatus.Suspended;
        tenant.IsSuspended = true;
        tenant.SuspensionReason = "Suscripción cancelada en Stripe.";
        tenant.SuspendedAt = DateTime.UtcNow;

        _db.TenantSubscriptionHistories.Add(new TenantSubscriptionHistory
        {
            TenantId = localSub.TenantId,
            PreviousPlanId = localSub.SubscriptionPlanId,
            NewPlanId = localSub.SubscriptionPlanId,
            PreviousStatus = prevStatus,
            NewStatus = SubscriptionStatus.Cancelled,
            ChangeReason = "Cancelación desde Stripe webhook.",
            ChangedByUserId = null
        });

        await AddTransactionAsync(localSub.TenantId, localSub.Id, SaasTransactionType.Cancellation, SaasTransactionStatus.Succeeded,
            0, localSub.SubscriptionPlan?.Currency ?? "USD", sub.Id, null, null, "Suscripción cancelada", null, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleInvoicePaidAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (invoice?.SubscriptionId == null) return;

        var localSub = await _db.TenantSubscriptions.IgnoreQueryFilters()
            .Include(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == invoice.SubscriptionId && !s.IsDeleted, cancellationToken);
        if (localSub == null) return;

        var amount = (decimal)invoice.AmountPaid / 100m;
        var currency = invoice.Currency ?? "usd";

        await AddTransactionAsync(localSub.TenantId, localSub.Id, SaasTransactionType.PaymentSucceeded, SaasTransactionStatus.Succeeded,
            amount, currency, invoice.PaymentIntentId ?? invoice.Id, invoice.Id, invoice.PaymentIntentId,
            "Pago de factura", null, cancellationToken);

        await UpsertSaaSInvoiceAsync(localSub, invoice, SaaSInvoiceStatus.Paid, cancellationToken);

        // Webhook fuera de orden: una factura pagada de un período anterior puede llegar
        // DESPUÉS de que la suscripción fue cancelada. No reactivar en ese caso.
        if (localSub.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)
        {
            _logger.LogWarning(
                "Ignorando invoice.paid para suscripción {SubId} con estado {Status} — posible webhook fuera de orden",
                localSub.Id, localSub.Status);
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var tenant = await _db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Id == localSub.TenantId, cancellationToken);
        if (tenant.IsSuspended)
        {
            tenant.IsSuspended = false;
            tenant.SuspensionReason = null;
            tenant.SuspendedAt = null;
            tenant.CommercialStatus = TenantCommercialStatus.Active;
        }
        localSub.Status = SubscriptionStatus.Active;
        localSub.LastBillingSyncAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleInvoicePaymentFailedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (invoice?.SubscriptionId == null) return;

        var localSub = await _db.TenantSubscriptions.IgnoreQueryFilters()
            .Include(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == invoice.SubscriptionId && !s.IsDeleted, cancellationToken);
        if (localSub == null) return;

        var amount = (decimal)invoice.AmountDue / 100m;
        var reason = invoice.LastFinalizationError?.Message ?? "Pago fallido";

        await AddTransactionAsync(localSub.TenantId, localSub.Id, SaasTransactionType.PaymentFailed, SaasTransactionStatus.Failed,
            amount, invoice.Currency ?? "usd", invoice.PaymentIntentId ?? invoice.Id, invoice.Id, invoice.PaymentIntentId,
            "Pago fallido", reason, cancellationToken);

        await UpsertSaaSInvoiceAsync(localSub, invoice, SaaSInvoiceStatus.Failed, cancellationToken);

        localSub.Status = SubscriptionStatus.PastDue;
        localSub.LastBillingSyncAt = DateTime.UtcNow;

        var tenant = await _db.Tenants.IgnoreQueryFilters().Include(t => t.BillingProfile).FirstAsync(t => t.Id == localSub.TenantId, cancellationToken);
        tenant.CommercialStatus = TenantCommercialStatus.PastDue;

        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await _notifications.DispatchAsync(new DispatchRequest(
                localSub.TenantId,
                NotificationEventType.SubscriptionPaymentFailed,
                new Dictionary<string, object>
                {
                    ["Amount"] = amount,
                    ["Currency"] = invoice.Currency ?? "usd",
                    ["Reason"] = reason,
                    ["PlanName"] = localSub.SubscriptionPlan?.Name ?? ""
                },
                RecipientEmail: tenant.BillingProfile?.BillingEmail ?? tenant.Email,
                RelatedEntityType: "TenantSubscription",
                RelatedEntityId: localSub.Id.ToString()), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SubscriptionPaymentFailed notification for tenant {TenantId}", localSub.TenantId);
        }
    }

    private async Task HandleInvoiceFinalizedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (invoice?.SubscriptionId == null) return;

        var localSub = await _db.TenantSubscriptions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == invoice.SubscriptionId && !s.IsDeleted, cancellationToken);
        if (localSub == null) return;

        await UpsertSaaSInvoiceAsync(localSub, invoice, invoice.Status == "paid" ? SaaSInvoiceStatus.Paid : SaaSInvoiceStatus.Open, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncSubscriptionFromStripeAsync(Guid tenantId, Guid planId, Subscription sub, CancellationToken cancellationToken)
    {
        var localSub = await _db.TenantSubscriptions.IgnoreQueryFilters()
            .Include(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == sub.Id && !s.IsDeleted, cancellationToken);

        var item = sub.Items?.Data?.FirstOrDefault();
        var priceId = item?.Price?.Id ?? "";
        var productId = item?.Price?.ProductId;
        var periodStart = ToUtc(sub.CurrentPeriodStart);
        var periodEnd = ToUtc(sub.CurrentPeriodEnd);

        var status = sub.Status switch
        {
            "active" => SubscriptionStatus.Active,
            "trialing" => SubscriptionStatus.Trial,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" or "unpaid" => SubscriptionStatus.Cancelled,
            _ => SubscriptionStatus.Active
        };

        if (localSub != null)
        {
            localSub.Status = status;
            localSub.ExternalPriceId = priceId;
            localSub.ExternalProductId = productId;
            localSub.CurrentPeriodStart = periodStart;
            localSub.CurrentPeriodEnd = periodEnd;
            localSub.CancelAtPeriodEnd = sub.CancelAtPeriodEnd;
            localSub.LastBillingSyncAt = DateTime.UtcNow;
        }
        else
        {
            var plan = await _db.SubscriptionPlans.IgnoreQueryFilters().FirstAsync(p => p.Id == planId, cancellationToken);
            localSub = new TenantSubscription
            {
                TenantId = tenantId,
                SubscriptionPlanId = planId,
                Status = status,
                StartDate = periodStart,
                ExternalSubscriptionId = sub.Id,
                ExternalPriceId = priceId,
                ExternalProductId = productId,
                ExternalPlanId = priceId,
                BillingProvider = BillingProvider.Stripe,
                BillingPeriod = item?.Price?.Recurring?.Interval == "year" ? BillingPeriod.Annual : BillingPeriod.Monthly,
                CurrentPeriodStart = periodStart,
                CurrentPeriodEnd = periodEnd,
                NextBillingDate = periodEnd,
                CancelAtPeriodEnd = sub.CancelAtPeriodEnd,
                LastBillingSyncAt = DateTime.UtcNow
            };
            // EF Core asigna el GUID de localSub.Id en Add() (generación client-side).
            // No se necesita SaveChangesAsync aquí: el tenant y el historial se guardan
            // junto con la suscripción en el SaveChangesAsync final, de forma atómica.
            _db.TenantSubscriptions.Add(localSub);

            var tenant = await _db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Id == tenantId, cancellationToken);
            tenant.CurrentSubscriptionId = localSub.Id;
            tenant.CommercialStatus = status == SubscriptionStatus.Trial ? TenantCommercialStatus.Trial : TenantCommercialStatus.Active;

            _db.TenantSubscriptionHistories.Add(new TenantSubscriptionHistory
            {
                TenantId = tenantId,
                PreviousPlanId = null,
                NewPlanId = planId,
                PreviousStatus = null,
                NewStatus = status,
                ChangeReason = "Suscripción creada desde Stripe.",
                ChangedByUserId = null
            });
        }

        // Un único SaveChangesAsync: garantiza atomicidad entre suscripción, tenant e historial.
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task AddTransactionAsync(Guid tenantId, Guid? subId, SaasTransactionType type, SaasTransactionStatus status,
        decimal amount, string currency, string? providerTxId, string? providerInvId, string? providerPiId,
        string? description, string? failureReason, CancellationToken ct)
    {
        if (await _db.SaaSBillingTransactions.IgnoreQueryFilters()
            .AnyAsync(t => t.ProviderTransactionId == providerTxId && !t.IsDeleted, ct))
            return;

        _db.SaaSBillingTransactions.Add(new SaaSBillingTransaction
        {
            TenantId = tenantId,
            TenantSubscriptionId = subId,
            TransactionType = type,
            Status = status,
            Amount = amount,
            Currency = currency.ToUpperInvariant(),
            ProviderTransactionId = providerTxId,
            ProviderInvoiceId = providerInvId,
            ProviderPaymentIntentId = providerPiId,
            Description = description,
            FailureReason = failureReason,
            OccurredAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertSaaSInvoiceAsync(TenantSubscription localSub, Stripe.Invoice inv, SaaSInvoiceStatus status, CancellationToken ct)
    {
        var existing = await _db.SaaSInvoices.IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.ProviderInvoiceId == inv.Id && !i.IsDeleted, ct);
        if (existing != null)
        {
            existing.Status = status;
            existing.TotalAmount = (decimal)(inv.AmountPaid > 0 ? inv.AmountPaid : inv.AmountDue) / 100m;
            existing.InvoiceUrl = inv.HostedInvoiceUrl;
            return;
        }

        var invNum = inv.Number ?? $"INV-{inv.Id[..12]}";
        if (await _db.SaaSInvoices.IgnoreQueryFilters().AnyAsync(i => i.InvoiceNumber == invNum, ct))
            invNum = $"INV-{inv.Id}-{Guid.NewGuid():N}"[..64];

        var periodStart = ToUtc(inv.PeriodStart);
        var periodEnd = ToUtc(inv.PeriodEnd);
        var issueDate = ToUtc(inv.Created);

        _db.SaaSInvoices.Add(new SaaSInvoice
        {
            TenantId = localSub.TenantId,
            TenantSubscriptionId = localSub.Id,
            InvoiceNumber = invNum,
            BillingPeriodStart = periodStart,
            BillingPeriodEnd = periodEnd,
            IssueDate = issueDate,
            DueDate = inv.DueDate.HasValue ? ToUtc(inv.DueDate!.Value) : (DateTime?)null,
            Subtotal = (decimal)inv.Subtotal / 100m,
            TaxAmount = inv.Tax.HasValue ? (decimal)inv.Tax.Value / 100m : 0m,
            DiscountAmount = 0,
            TotalAmount = (decimal)(inv.AmountPaid > 0 ? inv.AmountPaid : inv.AmountDue) / 100m,
            Currency = (inv.Currency ?? "usd").ToUpperInvariant(),
            Status = status,
            ProviderInvoiceId = inv.Id,
            InvoiceUrl = inv.HostedInvoiceUrl
        });
    }

    private static DateTime ToUtc(DateTime dt) =>
        dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();

    private static Guid? GetGuidFromMetadata(Dictionary<string, string>? metadata, string key)
    {
        if (metadata == null || !metadata.TryGetValue(key, out var val) || string.IsNullOrEmpty(val))
            return null;
        return Guid.TryParse(val, out var g) ? g : null;
    }

    /// <summary>
    /// Detecta si una excepción de EF Core es una violación de restricción única de PostgreSQL.
    /// El índice único en ProviderEventId garantiza que solo una instancia procese cada webhook.
    /// </summary>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pgEx
               && pgEx.SqlState == "23505"; // unique_violation
    }
}
