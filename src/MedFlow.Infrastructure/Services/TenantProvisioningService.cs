using System.Text.Json;
using MedFlow.Application.Interfaces;
using MedFlow.Application.Onboarding;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Identity;
using MedFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedFlow.Infrastructure.Services;

public sealed class TenantProvisioningService : ITenantProvisioningService
{
    private const string EventTenantCreated = "TenantCreated";
    private const string EventAdminUserCreated = "AdminUserCreated";
    private const string EventSubscriptionStarted = "SubscriptionStarted";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ITenantContext _tenant;
    private readonly ILogger<TenantProvisioningService> _logger;

    public TenantProvisioningService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ITenantContext tenant,
        ILogger<TenantProvisioningService> logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<TenantProvisioningResult> ProvisionAsync(
        TenantProvisioningRequest request,
        string? clientIp,
        CancellationToken cancellationToken = default)
    {
        var prevIgnore = _tenant.IgnoreTenantFilter;
        _tenant.SetIgnoreTenantFilter(true);
        try
        {
            var errors = await ValidateAsync(request, cancellationToken).ConfigureAwait(false);
            if (errors.Count > 0)
                return TenantProvisioningResult.Fail(errors);

            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var code = NormalizeCode(request.Code);
                    if (await _db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Code == code && !t.IsDeleted, cancellationToken).ConfigureAwait(false))
                    {
                        await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                        return TenantProvisioningResult.Fail("El código de clínica ya está en uso.");
                    }

                    var plan = await _db.SubscriptionPlans.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(p => p.Id == request.SubscriptionPlanId && !p.IsDeleted, cancellationToken)
                        .ConfigureAwait(false);
                    if (plan == null || !plan.IsActive)
                    {
                        await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                        return TenantProvisioningResult.Fail("El plan seleccionado no está disponible.");
                    }

                    const string defaultPrimary = "#3c8dbc";
                    const string defaultSecondary = "#001f3f";

                    var tenant = new Tenant
                    {
                        Name = request.ClinicName.Trim(),
                        Code = code,
                        Email = string.IsNullOrWhiteSpace(request.ClinicEmail) ? null : request.ClinicEmail.Trim(),
                        Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                        Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
                        PrimaryColor = defaultPrimary,
                        SecondaryColor = defaultSecondary,
                        LogoUrl = null,
                        CommercialStatus = request.StartWithTrial ? TenantCommercialStatus.Trial : TenantCommercialStatus.Active,
                        ActivatedAt = DateTime.UtcNow,
                        IsSuspended = false
                    };

                    _db.Tenants.Add(tenant);
                    await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    var now = DateTime.UtcNow;
                    var subStatus = request.StartWithTrial && plan.TrialDays > 0
                        ? SubscriptionStatus.Trial
                        : SubscriptionStatus.Active;

                    var sub = new TenantSubscription
                    {
                        TenantId = tenant.Id,
                        SubscriptionPlanId = plan.Id,
                        StartDate = now,
                        Status = subStatus,
                        TrialStartDate = subStatus == SubscriptionStatus.Trial ? now : null,
                        TrialEndDate = subStatus == SubscriptionStatus.Trial ? now.AddDays(plan.TrialDays) : null,
                        EndDate = null,
                        NextBillingDate = now.AddMonths(1)
                    };

                    _db.TenantSubscriptions.Add(sub);
                    await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    tenant.CurrentSubscriptionId = sub.Id;
                    tenant.CommercialStatus = sub.Status == SubscriptionStatus.Trial
                        ? TenantCommercialStatus.Trial
                        : TenantCommercialStatus.Active;
                    _db.Tenants.Update(tenant);
                    await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    _db.TenantSubscriptionHistories.Add(new TenantSubscriptionHistory
                    {
                        TenantId = tenant.Id,
                        PreviousPlanId = null,
                        NewPlanId = plan.Id,
                        PreviousStatus = null,
                        NewStatus = sub.Status,
                        ChangeReason = "Onboarding automático: alta de clínica y plan inicial.",
                        ChangedByUserId = null
                    });
                    await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    var specialitiesJson = JsonSerializer.Serialize(DefaultSpecialities, JsonOptions);
                    foreach (var (key, value) in BuildSettingsMap(request, defaultPrimary, defaultSecondary, specialitiesJson))
                    {
                        _db.TenantSettings.Add(new TenantSettings
                        {
                            TenantId = tenant.Id,
                            SettingKey = key,
                            SettingValue = value,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    var adminEmail = request.AdminEmail.Trim();
                    var user = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        FirstName = request.AdminFirstName.Trim(),
                        LastName = request.AdminLastName.Trim(),
                        FullName = $"{request.AdminFirstName.Trim()} {request.AdminLastName.Trim()}".Trim(),
                        TenantId = tenant.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var createResult = await _userManager.CreateAsync(user, request.AdminPassword).ConfigureAwait(false);
                    if (!createResult.Succeeded)
                    {
                        await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                        return TenantProvisioningResult.Fail(createResult.Errors.Select(e => e.Description).ToList());
                    }

                    var adminRole = await _roleManager.FindByNameAsync("Admin").ConfigureAwait(false);
                    if (adminRole == null)
                    {
                        await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                        return TenantProvisioningResult.Fail("Rol Admin no disponible en el sistema.");
                    }

                    var roleResult = await _userManager.AddToRoleAsync(user, "Admin").ConfigureAwait(false);
                    if (!roleResult.Succeeded)
                    {
                        await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                        return TenantProvisioningResult.Fail(roleResult.Errors.Select(e => e.Description).ToList());
                    }

                    await WriteAuditAsync(
                        tenant.Id,
                        user.Id,
                        user.UserName,
                        clientIp,
                        "Tenant.Create",
                        "Onboarding",
                        nameof(Tenant),
                        tenant.Id.ToString(),
                        $"Alta de clínica {tenant.Name} ({tenant.Code})",
                        null,
                        JsonSerializer.Serialize(new { tenantName = tenant.Name, tenantCode = tenant.Code, planName = plan.Name }, JsonOptions),
                        cancellationToken).ConfigureAwait(false);

                    await WriteAuditAsync(
                        tenant.Id,
                        user.Id,
                        user.UserName,
                        clientIp,
                        "User.Create",
                        "Onboarding",
                        nameof(ApplicationUser),
                        user.Id,
                        $"Usuario administrador {adminEmail}",
                        null,
                        JsonSerializer.Serialize(new { user.Email, Role = "Admin" }, JsonOptions),
                        cancellationToken).ConfigureAwait(false);

                    await WriteAuditAsync(
                        tenant.Id,
                        user.Id,
                        user.UserName,
                        clientIp,
                        "Subscription.Assign",
                        "Onboarding",
                        nameof(TenantSubscription),
                        sub.Id.ToString(),
                        $"Plan {plan.Name} ({plan.Code})",
                        null,
                        JsonSerializer.Serialize(new { plan.Id, plan.Name, sub.Status }, JsonOptions),
                        cancellationToken).ConfigureAwait(false);

                    await WriteEventAsync(tenant.Id, EventTenantCreated, new
                    {
                        tenantId = tenant.Id,
                        code = tenant.Code,
                        name = tenant.Name,
                        email = tenant.Email,
                        commercialStatus = tenant.CommercialStatus.ToString(),
                        occurredAtUtc = DateTime.UtcNow
                    }, nameof(Tenant), tenant.Id.ToString(), cancellationToken).ConfigureAwait(false);

                    await WriteEventAsync(tenant.Id, EventSubscriptionStarted, new
                    {
                        tenantId = tenant.Id,
                        subscriptionId = sub.Id,
                        planId = plan.Id,
                        planCode = plan.Code,
                        planName = plan.Name,
                        status = sub.Status.ToString(),
                        trialEnd = sub.TrialEndDate,
                        occurredAtUtc = DateTime.UtcNow
                    }, nameof(TenantSubscription), sub.Id.ToString(), cancellationToken).ConfigureAwait(false);

                    await WriteEventAsync(tenant.Id, EventAdminUserCreated, new
                    {
                        tenantId = tenant.Id,
                        userId = user.Id,
                        email = adminEmail,
                        role = "Admin",
                        occurredAtUtc = DateTime.UtcNow
                    }, nameof(ApplicationUser), user.Id, cancellationToken).ConfigureAwait(false);

                    await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

                    return TenantProvisioningResult.Ok(tenant.Id, tenant.Code, user.Id, request.StartWithTrial && plan.TrialDays > 0);
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogError(ex, "Error en provisionamiento de tenant");
                    return TenantProvisioningResult.Fail("No se pudo completar el alta. Intente de nuevo o contacte a soporte.");
                }
            }).ConfigureAwait(false);
        }
        finally
        {
            _tenant.SetIgnoreTenantFilter(prevIgnore);
        }
    }

    private async Task<List<string>> ValidateAsync(TenantProvisioningRequest request, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.ClinicName))
            errors.Add("El nombre de la clínica es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.Code))
            errors.Add("El código de clínica es obligatorio.");
        else if (!IsValidCode(request.Code))
            errors.Add("El código solo puede contener letras minúsculas, números y guiones (3-64 caracteres).");

        if (string.IsNullOrWhiteSpace(request.AdminFirstName))
            errors.Add("El nombre del administrador es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.AdminLastName))
            errors.Add("El apellido del administrador es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.AdminEmail))
            errors.Add("El correo del administrador es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.AdminPassword))
            errors.Add("La contraseña es obligatoria.");

        if (string.IsNullOrWhiteSpace(request.TimeZoneId))
            errors.Add("La zona horaria es obligatoria.");
        if (string.IsNullOrWhiteSpace(request.DateFormat))
            errors.Add("El formato de fecha es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Currency))
            errors.Add("La moneda es obligatoria.");
        if (string.IsNullOrWhiteSpace(request.Language))
            errors.Add("El idioma es obligatorio.");

        if (request.SubscriptionPlanId == Guid.Empty)
            errors.Add("Debe seleccionar un plan.");

        if (errors.Count > 0)
            return errors;

        var code = NormalizeCode(request.Code);
        if (await _db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Code == code && !t.IsDeleted, cancellationToken).ConfigureAwait(false))
            errors.Add("Ya existe una clínica con ese código.");

        var plan = await _db.SubscriptionPlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == request.SubscriptionPlanId && !p.IsDeleted, cancellationToken)
            .ConfigureAwait(false);
        if (plan == null)
            errors.Add("Plan no encontrado.");
        else if (!plan.IsActive)
            errors.Add("El plan seleccionado no está activo.");

        var email = request.AdminEmail.Trim();
        var tempUser = new ApplicationUser { UserName = email, Email = email };
        foreach (var validator in _userManager.PasswordValidators)
        {
            var vr = await validator.ValidateAsync(_userManager, tempUser, request.AdminPassword).ConfigureAwait(false);
            if (!vr.Succeeded)
                errors.AddRange(vr.Errors.Select(e => e.Description));
        }

        var normEmail = _userManager.NormalizeEmail(email);
        if (normEmail != null)
        {
            var emailTaken = await _db.Users.IgnoreQueryFilters()
                .AnyAsync(u => u.NormalizedEmail == normEmail, cancellationToken)
                .ConfigureAwait(false);
            if (emailTaken)
                errors.Add("Ya existe un usuario registrado con ese correo electrónico.");
        }

        return errors;
    }

    private async Task WriteAuditAsync(
        Guid tenantId,
        string? userId,
        string? userName,
        string? ip,
        string action,
        string module,
        string? entityName,
        string? entityId,
        string? description,
        string? oldJson,
        string? newJson,
        CancellationToken cancellationToken)
    {
        await _db.AuditLogs.AddAsync(new AuditLog
        {
            TenantId = tenantId,
            UserId = userId,
            UserName = userName,
            Action = action,
            Module = module,
            EntityName = entityName,
            EntityId = entityId,
            Description = description,
            OldValuesJson = oldJson,
            NewValuesJson = newJson,
            IpAddress = ip,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteEventAsync(
        Guid tenantId,
        string eventType,
        object payload,
        string aggregateType,
        string aggregateId,
        CancellationToken cancellationToken)
    {
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        await _db.EventLogs.AddAsync(new EventLog
        {
            TenantId = tenantId,
            EventType = eventType,
            PayloadJson = payloadJson,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            Status = OutboxEventStatus.Pending,
            RetryCount = 0,
            ProcessedAt = null,
            LastError = null
        }, cancellationToken).ConfigureAwait(false);
    }

    private static IEnumerable<(string Key, string? Value)> BuildSettingsMap(
        TenantProvisioningRequest request,
        string primary,
        string secondary,
        string specialitiesJson)
    {
        yield return (TenantSettingKeys.CoreTimeZone, request.TimeZoneId.Trim());
        yield return (TenantSettingKeys.CoreDateFormat, request.DateFormat.Trim());
        yield return (TenantSettingKeys.CoreCurrency, request.Currency.Trim());
        yield return (TenantSettingKeys.CoreLanguage, request.Language.Trim());
        yield return (TenantSettingKeys.BrandingPrimaryColor, primary);
        yield return (TenantSettingKeys.BrandingSecondaryColor, secondary);
        yield return (TenantSettingKeys.BrandingLogoUrl, "");
        yield return (TenantSettingKeys.CatalogDefaultSpecialities, specialitiesJson);
    }

    private static IReadOnlyList<string> DefaultSpecialities { get; } =
    [
        "Medicina General",
        "Pediatría",
        "Ginecología",
        "Cardiología",
        "Dermatología",
        "Traumatología",
        "Psiquiatría",
        "Oftalmología",
        "Otorrinolaringología",
        "Nutrición"
    ];

    private static bool IsValidCode(string code)
    {
        var c = code.Trim();
        if (c.Length is < 3 or > 64)
            return false;
        foreach (var ch in c)
        {
            if (char.IsAsciiLetterLower(ch) || char.IsAsciiDigit(ch) || ch == '-')
                continue;
            return false;
        }

        return true;
    }

    private static string NormalizeCode(string code) =>
        code.Trim().ToLowerInvariant().Replace(' ', '-');
}
