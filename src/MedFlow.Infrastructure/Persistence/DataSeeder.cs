using MedFlow.Application.Security;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedFlow.Infrastructure.Persistence;

public class DataSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger)
    {
        await SeedPermissionsAsync(context, logger);
        await SeedRolesAsync(roleManager, logger);
        await SeedRolePermissionsAsync(context, roleManager, logger);
        await SyncSuperAdminAdminPermissionsAsync(context, roleManager, logger);
        await SyncMatchingSubsetRolesAsync(context, roleManager, logger);
        await SeedTenantAsync(context, logger);
        await SeedSubscriptionPlansAsync(context, logger);
        await SeedDemoSubscriptionAsync(context, logger);
        await SeedExampleWorkflowsAsync(context, logger);
        await SeedAdminUserAsync(userManager, roleManager, context, logger);
        await SeedSuperAdminUserAsync(userManager, roleManager, context, logger);
        await SeedRoleBasedUsersAsync(userManager, roleManager, context, logger);

        // Asegura datos clínicos mínimos para que flujos UI E2E (citas/consultas) no fallen
        // por ausencia de doctores activos en el tenant demo.
        await SeedDemoActiveDoctorsAsync(context, logger);
    }

    private static async Task SeedExampleWorkflowsAsync(ApplicationDbContext context, ILogger logger)
    {
        var tenantId = await context.Tenants.IgnoreQueryFilters()
            .Where(t => t.Code == "demo" && !t.IsDeleted)
            .Select(t => t.Id)
            .FirstOrDefaultAsync();
        if (tenantId == Guid.Empty) return;
        if (await context.WorkflowDefinitions.IgnoreQueryFilters().AnyAsync(w => w.TenantId == tenantId && !w.IsDeleted)) return;

        // Las URLs de webhook quedan vacías para forzar configuración explícita por el administrador.
        // Los workflows están inactivos por defecto; deben configurarse antes de activarse.
        var examples = new[]
        {
            new WorkflowDefinition
            {
                TenantId = tenantId,
                Name = "Recordatorio de cita (24h antes)",
                Code = "appointment-reminder-24h",
                Description = "Envía recordatorio al paciente 24 horas antes de la cita",
                TriggerEvent = "AppointmentReminder",
                IsActive = false,
                WebhookUrl = "",
                HttpMethod = "POST",
                PayloadTemplateJson = "{\"eventType\":\"{{EventType}}\",\"appointmentId\":\"{{AggregateId}}\",\"patientName\":\"{{PatientName}}\",\"date\":\"{{AppointmentDate}}\",\"time\":\"{{AppointmentTime}}\",\"clinicName\":\"{{ClinicName}}\"}",
                RetryPolicyJson = "{\"maxAttempts\":3,\"delaySeconds\":60,\"backoffType\":\"exponential\",\"multiplier\":2}",
                TimeoutSeconds = 30
            },
            new WorkflowDefinition
            {
                TenantId = tenantId,
                Name = "Confirmación automática de cita",
                Code = "appointment-confirmation",
                Description = "Confirma cita cuando el paciente responde",
                TriggerEvent = "AppointmentConfirmed",
                IsActive = false,
                WebhookUrl = "",
                HttpMethod = "POST",
                PayloadTemplateJson = "{\"eventType\":\"{{EventType}}\",\"appointmentId\":\"{{AggregateId}}\",\"payload\":{{Payload}}}",
                TimeoutSeconds = 30
            },
            new WorkflowDefinition
            {
                TenantId = tenantId,
                Name = "Seguimiento de paciente inactivo",
                Code = "patient-inactive-followup",
                Description = "Alerta cuando un paciente no tiene actividad reciente",
                TriggerEvent = "PatientInactive",
                IsActive = false,
                WebhookUrl = "",
                HttpMethod = "POST",
                PayloadTemplateJson = "{\"eventType\":\"{{EventType}}\",\"patientId\":\"{{AggregateId}}\",\"patientName\":\"{{PatientName}}\",\"lastActivity\":\"{{LastActivityDate}}\"}",
                RetryPolicyJson = "{\"maxAttempts\":2,\"delaySeconds\":300,\"backoffType\":\"simple\"}",
                TimeoutSeconds = 30
            },
            new WorkflowDefinition
            {
                TenantId = tenantId,
                Name = "Aviso de factura pendiente",
                Code = "invoice-pending-alert",
                Description = "Notifica facturas pendientes de pago",
                TriggerEvent = "InvoicePending",
                IsActive = false,
                WebhookUrl = "",
                HttpMethod = "POST",
                PayloadTemplateJson = "{\"eventType\":\"{{EventType}}\",\"invoiceId\":\"{{AggregateId}}\",\"invoiceAmount\":\"{{InvoiceAmount}}\",\"patientName\":\"{{PatientName}}\",\"dueDate\":\"{{DueDate}}\"}",
                RetryPolicyJson = "{\"maxAttempts\":3,\"delaySeconds\":120,\"backoffType\":\"exponential\",\"multiplier\":2}",
                TimeoutSeconds = 30
            },
            new WorkflowDefinition
            {
                TenantId = tenantId,
                Name = "Aviso de pago recibido",
                Code = "payment-received",
                Description = "Notifica internamente cuando se registra un pago",
                TriggerEvent = "PaymentRegistered",
                IsActive = false,
                WebhookUrl = "",
                HttpMethod = "POST",
                PayloadTemplateJson = "{\"eventType\":\"{{EventType}}\",\"paymentId\":\"{{PaymentId}}\",\"amount\":\"{{Amount}}\",\"invoiceId\":\"{{InvoiceId}}\"}",
                TimeoutSeconds = 30
            },
            new WorkflowDefinition
            {
                TenantId = tenantId,
                Name = "Alerta de pago SaaS fallido",
                Code = "saas-payment-failed",
                Description = "Alerta crítica cuando falla el pago de la suscripción SaaS",
                TriggerEvent = "SaaSPaymentFailed",
                IsActive = false,
                WebhookUrl = "",
                HttpMethod = "POST",
                PayloadTemplateJson = "{\"eventType\":\"{{EventType}}\",\"tenantId\":\"{{TenantId}}\",\"subscriptionStatus\":\"{{SubscriptionStatus}}\",\"amount\":\"{{Amount}}\"}",
                RetryPolicyJson = "{\"maxAttempts\":5,\"delaySeconds\":60,\"backoffType\":\"exponential\",\"multiplier\":3}",
                TimeoutSeconds = 60
            },
            new WorkflowDefinition
            {
                TenantId = tenantId,
                Name = "Notificación interna a recepción",
                Code = "reception-internal-notification",
                Description = "Avisa a recepción cuando se crea una nueva cita",
                TriggerEvent = "AppointmentCreated",
                IsActive = false,
                WebhookUrl = "",
                HttpMethod = "POST",
                PayloadTemplateJson = "{\"eventType\":\"{{EventType}}\",\"appointmentId\":\"{{AggregateId}}\",\"patientName\":\"{{PatientName}}\",\"doctorName\":\"{{DoctorName}}\",\"appointmentDate\":\"{{AppointmentDate}}\",\"appointmentTime\":\"{{AppointmentTime}}\",\"clinicName\":\"{{ClinicName}}\"}",
                HeadersJson = "{\"X-Source\":\"MedFlow\",\"Content-Type\":\"application/json\"}",
                TimeoutSeconds = 30
            },
            new WorkflowDefinition
            {
                TenantId = tenantId,
                Name = "Envío de resumen diario",
                Code = "daily-summary-report",
                Description = "Resumen diario de citas, pagos y actividad (disparado por job programado)",
                TriggerEvent = "DailySummary",
                IsActive = false,
                WebhookUrl = "",
                HttpMethod = "POST",
                PayloadTemplateJson = "{\"eventType\":\"{{EventType}}\",\"tenantId\":\"{{TenantId}}\",\"date\":\"{{SummaryDate}}\",\"appointmentsCount\":\"{{AppointmentsCount}}\",\"paymentsTotal\":\"{{PaymentsTotal}}\"}",
                RetryPolicyJson = "{\"maxAttempts\":2,\"delaySeconds\":300,\"backoffType\":\"simple\"}",
                TimeoutSeconds = 120
            }
        };

        context.WorkflowDefinitions.AddRange(examples);
        await context.SaveChangesAsync();
        logger.LogInformation("Ejemplos de workflows creados para tenant demo.");
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext context, ILogger logger)
    {
        var existingCodes = await context.Permissions.Select(p => p.Code).ToListAsync();
        var added = 0;
        foreach (var (code, name, module) in PermissionCodes.All)
        {
            if (existingCodes.Contains(code)) continue;
            context.Permissions.Add(new Permission
            {
                Code = code,
                Name = name,
                Module = module,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Permisos: {Added} nuevos (catálogo {Total}).", added, PermissionCodes.All.Count);
        }
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        var roles = new (string Name, string? Description)[]
        {
            ("SuperAdmin", "Acceso total al sistema"),
            ("Admin", "Administración clínica"),
            ("Reception", "Recepción y agenda"),
            ("Doctor", "Personal médico"),
            ("Billing", "Facturación y cobros"),
            ("Staff", "Personal general"),
            ("Patient", "Portal del paciente"),
        };

        foreach (var (name, desc) in roles)
        {
            if (await roleManager.RoleExistsAsync(name)) continue;
            var r = new ApplicationRole
            {
                Name = name,
                NormalizedName = name.ToUpperInvariant(),
                Description = desc,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await roleManager.CreateAsync(r);
            logger.LogInformation("Created role: {Role}", name);
        }
    }

    private static async Task SeedRolePermissionsAsync(ApplicationDbContext context, RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        var allPerms = await context.Permissions.AsNoTracking().Where(p => !p.IsDeleted).ToListAsync();
        if (allPerms.Count == 0) return;

        async Task AssignAllToRole(string roleName)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null) return;
            var existing = await context.RolePermissions.AnyAsync(rp => rp.RoleId == role.Id);
            if (existing) return;
            foreach (var p in allPerms)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = p.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();
            logger.LogInformation("Assigned all permissions to {Role}", roleName);
        }

        await AssignAllToRole("SuperAdmin");
        await AssignAllToRole("Admin");

        async Task AssignSubset(string roleName, Func<string, bool> codePredicate)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null) return;
            var has = await context.RolePermissions.AnyAsync(rp => rp.RoleId == role.Id);
            if (has) return;
            foreach (var p in allPerms.Where(x => codePredicate(x.Code)))
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = p.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();
        }

        await AssignSubset("Doctor", c => c.StartsWith("patients.", StringComparison.Ordinal) || c.StartsWith("appointments.", StringComparison.Ordinal) || c.StartsWith("medical_records.", StringComparison.Ordinal) || c.StartsWith("doctors.", StringComparison.Ordinal) || c == PermissionCodes.DashboardView || c == PermissionCodes.ReportsView);

        await AssignSubset("Reception", c => c.StartsWith("patients.", StringComparison.Ordinal) || c.StartsWith("appointments.", StringComparison.Ordinal) || c == PermissionCodes.DashboardView || c == PermissionCodes.ReportsView);

        await AssignSubset("Billing", c => c.StartsWith("billing.", StringComparison.Ordinal) || c.StartsWith("cash.", StringComparison.Ordinal) || c == PermissionCodes.DashboardView || c == PermissionCodes.ReportsView);

        await AssignSubset("Staff", c =>
            c == PermissionCodes.DashboardView
            || c == PermissionCodes.PatientsView
            || c == PermissionCodes.DoctorsView
            || c.StartsWith("appointments.", StringComparison.Ordinal));
    }

    /// <summary>Añade a SuperAdmin/Admin cualquier permiso nuevo del catálogo.</summary>
    private static async Task SyncSuperAdminAdminPermissionsAsync(ApplicationDbContext context, RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        var allPerms = await context.Permissions.AsNoTracking().Where(p => !p.IsDeleted).ToListAsync();
        if (allPerms.Count == 0) return;

        foreach (var roleName in new[] { "SuperAdmin", "Admin" })
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null) continue;
            var have = await context.RolePermissions.Where(rp => rp.RoleId == role.Id).Select(rp => rp.PermissionId).ToListAsync();
            var missing = allPerms.Where(p => !have.Contains(p.Id)).ToList();
            if (missing.Count == 0) continue;
            foreach (var p in missing)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = p.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Sincronizados {N} permisos con rol {Role}", missing.Count, roleName);
        }
    }

    private static async Task SyncMatchingSubsetRolesAsync(ApplicationDbContext context, RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        var allPerms = await context.Permissions.AsNoTracking().Where(p => !p.IsDeleted).ToListAsync();
        if (allPerms.Count == 0) return;

        async Task Sync(string roleName, Func<string, bool> codePredicate)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null) return;

            var allowedIds = allPerms.Where(p => codePredicate(p.Code)).Select(p => p.Id).ToHashSet();

            var existingIds = await context.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            // Seguridad: elimina permisos fuera del subset permitido para cerrar bypasses.
            var idsToRemove = existingIds.Where(id => !allowedIds.Contains(id)).ToList();
            if (idsToRemove.Count > 0)
            {
                var toRemove = context.RolePermissions.Where(rp => rp.RoleId == role.Id && idsToRemove.Contains(rp.PermissionId));
                context.RolePermissions.RemoveRange(toRemove);
            }

            var existingIdSet = existingIds.ToHashSet();
            var missing = allPerms.Where(p => allowedIds.Contains(p.Id) && !existingIdSet.Contains(p.Id)).ToList();
            if (missing.Count > 0)
            {
                foreach (var p in missing)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = p.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            if (idsToRemove.Count > 0 || missing.Count > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("Rol {Role}: subset sync (removed {R}, added {A}).", roleName, idsToRemove.Count, missing.Count);
            }
        }

        await Sync("Doctor", c => c.StartsWith("patients.", StringComparison.Ordinal) || c.StartsWith("appointments.", StringComparison.Ordinal) || c.StartsWith("medical_records.", StringComparison.Ordinal) || c.StartsWith("doctors.", StringComparison.Ordinal) || c == PermissionCodes.DashboardView || c == PermissionCodes.ReportsView);
        await Sync("Reception", c => c.StartsWith("patients.", StringComparison.Ordinal) || c.StartsWith("appointments.", StringComparison.Ordinal) || c == PermissionCodes.DashboardView || c == PermissionCodes.ReportsView);
        await Sync("Billing", c => c.StartsWith("billing.", StringComparison.Ordinal) || c.StartsWith("cash.", StringComparison.Ordinal) || c == PermissionCodes.DashboardView || c == PermissionCodes.ReportsView);
        await Sync("Staff", c =>
            c == PermissionCodes.DashboardView
            || c == PermissionCodes.PatientsView
            || c == PermissionCodes.DoctorsView
            || c.StartsWith("appointments.", StringComparison.Ordinal));
    }

    private static async Task SeedSubscriptionPlansAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.SubscriptionPlans.IgnoreQueryFilters().AnyAsync(p => !p.IsDeleted))
            return;

        var plans = new[]
        {
            new SubscriptionPlan
            {
                Name = "Starter",
                Code = "starter",
                Description = "Operación clínica esencial para equipos pequeños.",
                MonthlyPrice = 49,
                AnnualPrice = 490,
                Currency = "USD",
                MaxUsers = 5,
                MaxDoctors = 3,
                MaxPatients = 500,
                MaxAppointmentsPerMonth = 200,
                MaxBranches = 1,
                IncludesBillingModule = false,
                IncludesAutomationModule = false,
                IncludesReportsModule = true,
                IncludesPatientPortal = false,
                IncludesMultiBranch = false,
                IncludesAdvancedAnalytics = false,
                TrialDays = 14,
                SortOrder = 10,
                IsActive = true
            },
            new SubscriptionPlan
            {
                Name = "Professional",
                Code = "professional",
                Description = "Facturación, automatización y portal del paciente.",
                MonthlyPrice = 149,
                AnnualPrice = 1490,
                Currency = "USD",
                MaxUsers = 25,
                MaxDoctors = 15,
                MaxPatients = 10000,
                MaxAppointmentsPerMonth = 2000,
                MaxBranches = 3,
                IncludesBillingModule = true,
                IncludesAutomationModule = true,
                IncludesReportsModule = true,
                IncludesPatientPortal = true,
                IncludesMultiBranch = false,
                IncludesAdvancedAnalytics = false,
                TrialDays = 14,
                SortOrder = 20,
                IsActive = true
            },
            new SubscriptionPlan
            {
                Name = "Enterprise",
                Code = "enterprise",
                Description = "Capacidad ampliada y analítica avanzada (límites 0 = ilimitado en validación).",
                MonthlyPrice = 399,
                AnnualPrice = 3990,
                Currency = "USD",
                MaxUsers = 0,
                MaxDoctors = 0,
                MaxPatients = 0,
                MaxAppointmentsPerMonth = 0,
                MaxBranches = null,
                IncludesBillingModule = true,
                IncludesAutomationModule = true,
                IncludesReportsModule = true,
                IncludesPatientPortal = true,
                IncludesMultiBranch = true,
                IncludesAdvancedAnalytics = true,
                TrialDays = 14,
                SortOrder = 30,
                IsActive = true
            }
        };

        context.SubscriptionPlans.AddRange(plans);
        await context.SaveChangesAsync();
        logger.LogInformation("Planes SaaS de ejemplo: {Count}.", plans.Length);
    }

    private static async Task SeedDemoSubscriptionAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.TenantSubscriptions.IgnoreQueryFilters().AnyAsync(s => !s.IsDeleted))
            return;

        var tenantId = await context.Tenants.IgnoreQueryFilters()
            .Where(t => t.Code == "demo" && !t.IsDeleted)
            .Select(t => t.Id)
            .FirstOrDefaultAsync();
        if (tenantId == Guid.Empty)
        {
            logger.LogWarning("No hay tenant demo para asignar suscripción Enterprise.");
            return;
        }

        var enterprise = await context.SubscriptionPlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Code == "enterprise" && !p.IsDeleted);
        if (enterprise == null)
        {
            logger.LogWarning("Plan enterprise no encontrado; omitiendo suscripción demo.");
            return;
        }

        var now = DateTime.UtcNow;
        var sub = new TenantSubscription
        {
            TenantId = tenantId,
            SubscriptionPlanId = enterprise.Id,
            StartDate = now,
            Status = SubscriptionStatus.Active,
            NextBillingDate = now.AddMonths(1)
        };

        context.TenantSubscriptions.Add(sub);
        await context.SaveChangesAsync();

        var tenant = await context.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Id == tenantId);
        tenant.CurrentSubscriptionId = sub.Id;
        tenant.CommercialStatus = TenantCommercialStatus.Active;
        tenant.ActivatedAt = now;
        await context.SaveChangesAsync();

        context.TenantSubscriptionHistories.Add(new TenantSubscriptionHistory
        {
            TenantId = tenantId,
            PreviousPlanId = null,
            NewPlanId = enterprise.Id,
            PreviousStatus = null,
            NewStatus = SubscriptionStatus.Active,
            ChangeReason = "Asignación inicial (seed) — Enterprise."
        });
        await context.SaveChangesAsync();

        logger.LogInformation("Suscripción Enterprise asignada al tenant demo.");
    }

    private static async Task SeedTenantAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Tenants.IgnoreQueryFilters().AnyAsync(t => !t.IsDeleted)) return;

        var tenant = new Tenant
        {
            Name = "MedFlow AI — Clínica demo",
            Code = "demo",
            LegalName = "MedFlow AI S.A.",
            Address = "Av. Principal 123",
            Phone = "+1234567890",
            Email = "contacto@medflow.ai",
            PrimaryColor = "#3c8dbc",
            SecondaryColor = "#001f3f"
        };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        logger.LogInformation("Tenant demo creado (Code=demo).");
    }

    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext context,
        ILogger logger)
    {
        const string adminEmail = "admin@medflow.ai";
        // FindByEmailAsync respeta los query filters del DbContext (filtra por TenantId).
        // Durante el seed el contexto no tiene tenant, por lo que el usuario ya existente
        // queda invisible y se lanza 23505. Usamos query directa con IgnoreQueryFilters.
        var existingAdmin = await context.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.NormalizedEmail == adminEmail.ToUpperInvariant());
        if (existingAdmin) return;

        var tenantId = await context.Tenants.IgnoreQueryFilters()
            .Where(t => t.Code == "demo" && !t.IsDeleted)
            .Select(t => t.Id)
            .FirstOrDefaultAsync();
        if (tenantId == Guid.Empty)
        {
            logger.LogWarning("No se encontró tenant demo; omitiendo admin de clínica.");
            return;
        }

        var password = ResolveInitialPassword("ADMIN_INITIAL_PASSWORD", adminEmail, logger);

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Administrador",
            LastName = "Clínica",
            FullName = "Administrador Clínica",
            EmailConfirmed = true,
            IsActive = true,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to create admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        var adm = await roleManager.FindByNameAsync("Admin");
        if (adm != null)
            await userManager.AddToRoleAsync(admin, adm.Name!);
        else
            await userManager.AddToRoleAsync(admin, "Admin");

        logger.LogInformation("Usuario admin de clínica: {Email}", adminEmail);
    }

    private static async Task SeedSuperAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext context,
        ILogger logger)
    {
        const string email = "superadmin@medflow.ai";
        var existingSuperAdmin = await context.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.NormalizedEmail == email.ToUpperInvariant());
        if (existingSuperAdmin) return;

        var password = ResolveInitialPassword("SUPERADMIN_INITIAL_PASSWORD", email, logger);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Super",
            LastName = "Admin",
            FullName = "Super Administrador",
            EmailConfirmed = true,
            IsActive = true,
            TenantId = null,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogError("No se pudo crear superadmin: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        var role = await roleManager.FindByNameAsync("SuperAdmin");
        if (role != null)
            await userManager.AddToRoleAsync(user, role.Name!);

        logger.LogInformation("Usuario plataforma SuperAdmin: {Email}", email);
    }

    private static async Task SeedRoleBasedUsersAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext context,
        ILogger logger)
    {
        var tenantId = await context.Tenants.IgnoreQueryFilters()
            .Where(t => t.Code == "demo" && !t.IsDeleted)
            .Select(t => t.Id)
            .FirstOrDefaultAsync();
        if (tenantId == Guid.Empty)
        {
            logger.LogWarning("No se encontró tenant demo; omitiendo seed de usuarios por rol.");
            return;
        }

        var seeds = new (string Email, string FirstName, string LastName, string Role, string EnvVar)[]
        {
            ("reception@medflow.ai", "Recepción", "Demo", "Reception", "RECEPTION_INITIAL_PASSWORD"),
            ("doctor@medflow.ai", "Doctor", "Demo", "Doctor", "DOCTOR_INITIAL_PASSWORD"),
            ("billing@medflow.ai", "Billing", "Demo", "Billing", "BILLING_INITIAL_PASSWORD"),
            ("staff@medflow.ai", "Staff", "Demo", "Staff", "STAFF_INITIAL_PASSWORD"),
            ("patient@medflow.ai", "Paciente", "Demo", "Patient", "PATIENT_INITIAL_PASSWORD")
        };

        // Pre-cargar emails existentes ignorando query filters (evita 23505 por filtro de tenant).
        var existingEmailsList = await context.Users.IgnoreQueryFilters()
            .Select(u => u.NormalizedEmail)
            .ToListAsync();
        var existingEmails = new HashSet<string?>(existingEmailsList, StringComparer.OrdinalIgnoreCase);

        foreach (var seed in seeds)
        {
            if (existingEmails.Contains(seed.Email.ToUpperInvariant()))
                continue;

            var password = ResolveInitialPassword(seed.EnvVar, seed.Email, logger);
            var user = new ApplicationUser
            {
                UserName = seed.Email,
                Email = seed.Email,
                FirstName = seed.FirstName,
                LastName = seed.LastName,
                FullName = $"{seed.FirstName} {seed.LastName}",
                EmailConfirmed = true,
                IsActive = true,
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow
            };

            var create = await userManager.CreateAsync(user, password);
            if (!create.Succeeded)
            {
                logger.LogError("No se pudo crear usuario {Email}: {Errors}",
                    seed.Email, string.Join(", ", create.Errors.Select(e => e.Description)));
                continue;
            }

            var role = await roleManager.FindByNameAsync(seed.Role);
            var roleName = role?.Name ?? seed.Role;
            var addRole = await userManager.AddToRoleAsync(user, roleName);
            if (!addRole.Succeeded)
            {
                logger.LogError("No se pudo asignar rol {Role} a {Email}: {Errors}",
                    roleName, seed.Email, string.Join(", ", addRole.Errors.Select(e => e.Description)));
                continue;
            }

            logger.LogInformation("Usuario seed creado por rol {Role}: {Email}", roleName, seed.Email);
        }
    }

    /// <summary>
    /// Crea o actualiza usuarios <c>qa.*@medflow.local</c> en el tenant demo, uno por rol de clínica,
    /// para pruebas funcionales con contraseña conocida (solo Development).
    /// </summary>
    public static async Task SeedQaTenantRoleUsersAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        string password,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(password))
            return;

        var tenantId = await context.Tenants.IgnoreQueryFilters()
            .Where(t => t.Code == "demo" && !t.IsDeleted)
            .Select(t => t.Id)
            .FirstOrDefaultAsync();
        if (tenantId == Guid.Empty)
        {
            logger.LogWarning("Seed QA por rol: no hay tenant demo.");
            return;
        }

        var seeds = new (string Email, string FirstName, string LastName, string RoleName)[]
        {
            ("qa.admin@medflow.local", "QA", "Admin", "Admin"),
            ("qa.reception@medflow.local", "QA", "Recepción", "Reception"),
            ("qa.doctor@medflow.local", "QA", "Doctor", "Doctor"),
            ("qa.billing@medflow.local", "QA", "Facturación", "Billing"),
            ("qa.staff@medflow.local", "QA", "Staff", "Staff"),
            ("qa.patient@medflow.local", "QA", "Paciente", "Patient"),
        };

        foreach (var seed in seeds)
        {
            var role = await roleManager.FindByNameAsync(seed.RoleName);
            if (role == null)
            {
                logger.LogWarning("Seed QA: rol {Role} no existe, se omite {Email}.", seed.RoleName, seed.Email);
                continue;
            }

            var normalized = userManager.NormalizeEmail(seed.Email);
            var existing = await context.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized);

            ApplicationUser user;
            if (existing == null)
            {
                user = new ApplicationUser
                {
                    UserName = seed.Email,
                    Email = seed.Email,
                    FirstName = seed.FirstName,
                    LastName = seed.LastName,
                    FullName = $"{seed.FirstName} {seed.LastName}",
                    EmailConfirmed = true,
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedAt = DateTime.UtcNow
                };

                var create = await userManager.CreateAsync(user, password);
                if (!create.Succeeded)
                {
                    logger.LogError("Seed QA: no se pudo crear {Email}: {Errors}",
                        seed.Email, string.Join(", ", create.Errors.Select(e => e.Description)));
                    continue;
                }

                var addRole = await userManager.AddToRoleAsync(user, role.Name!);
                if (!addRole.Succeeded)
                {
                    logger.LogError("Seed QA: no se pudo asignar rol {Role} a {Email}: {Errors}",
                        seed.RoleName, seed.Email, string.Join(", ", addRole.Errors.Select(e => e.Description)));
                }
                else
                    logger.LogInformation("Seed QA: usuario creado {Email} → {Role}", seed.Email, seed.RoleName);
                continue;
            }

            user = existing;
            if (user.TenantId != tenantId)
            {
                user.TenantId = tenantId;
                var upd = await userManager.UpdateAsync(user);
                if (!upd.Succeeded)
                {
                    logger.LogError("Seed QA: no se pudo asignar tenant a {Email}: {Errors}",
                        seed.Email, string.Join(", ", upd.Errors.Select(e => e.Description)));
                    continue;
                }
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await userManager.ResetPasswordAsync(user, token, password);
            if (!reset.Succeeded)
            {
                logger.LogError("Seed QA: no se pudo restablecer contraseña de {Email}: {Errors}",
                    seed.Email, string.Join(", ", reset.Errors.Select(e => e.Description)));
                continue;
            }

            await userManager.SetLockoutEndDateAsync(user, null);
            await userManager.ResetAccessFailedCountAsync(user);

            if (!await userManager.IsInRoleAsync(user, role.Name!))
            {
                var current = await userManager.GetRolesAsync(user);
                if (current.Count > 0)
                    await userManager.RemoveFromRolesAsync(user, current);
                await userManager.AddToRoleAsync(user, role.Name!);
            }

            logger.LogInformation("Seed QA: contraseña y rol actualizados {Email} → {Role}", seed.Email, seed.RoleName);
        }

        await SeedQaPatientPortalPatientAsync(context, tenantId, logger);
    }

    private static async Task SeedQaPatientPortalPatientAsync(ApplicationDbContext context, Guid tenantId, ILogger logger)
    {
        const string email = "qa.patient@medflow.local";
        var user = await context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            logger.LogWarning("Seed QA paciente: no existe {Email}.", email);
            return;
        }

        var linked = await context.Patients.IgnoreQueryFilters()
            .AnyAsync(p => p.UserId == user.Id);
        if (linked)
            return;

        context.Patients.Add(new Patient
        {
            TenantId = tenantId,
            PrimerNombre = "QA",
            PrimerApellido = "Paciente",
            Correo = email,
            UserId = user.Id,
            NumeroDocumento = "QA-DOC-PATIENT-001",
            TipoDocumento = "OTRO"
        });
        await context.SaveChangesAsync();
        logger.LogInformation("Seed QA: registro Patient vinculado a {Email} para portal/API paciente.", email);
    }

    private static async Task SeedDemoActiveDoctorsAsync(ApplicationDbContext context, ILogger logger)
    {
        var tenantId = await context.Tenants.IgnoreQueryFilters()
            .Where(t => t.Code == "demo" && !t.IsDeleted)
            .Select(t => t.Id)
            .FirstOrDefaultAsync();

        if (tenantId == Guid.Empty) return;

        var hasActiveDoctors = await context.Doctors.IgnoreQueryFilters()
            .AnyAsync(d => d.TenantId == tenantId && !d.IsDeleted && d.IsActive);

        if (hasActiveDoctors)
            return;

        context.Doctors.Add(new Doctor
        {
            TenantId = tenantId,
            FirstName = "QA",
            LastName = "Doctor",
            ConsultationRoom = "D-101",
            IsActive = true,
            Notes = "Seed para pruebas E2E (apertura de selectores de doctor)."
        });

        await context.SaveChangesAsync();
        logger.LogInformation("Seed Demo: doctor activo creado para flujos E2E.");
    }

    /// <summary>
    /// Resuelve la contraseña inicial desde una variable de entorno.
    /// Si no está definida, genera una contraseña aleatoria segura y la registra
    /// en el log (solo ocurre en el primer arranque cuando el usuario no existe).
    /// En producción, establece la variable de entorno correspondiente.
    /// </summary>
    private static string ResolveInitialPassword(string envVarName, string userEmail, ILogger logger)
    {
        var password = Environment.GetEnvironmentVariable(envVarName);
        if (!string.IsNullOrWhiteSpace(password))
            return password;

        password = GenerateSecurePassword();
        // La contraseña se escribe en stderr (consola de arranque) y NO en los logs
        // para evitar que quede registrada en sistemas de logging centralizados.
        logger.LogWarning(
            "Variable de entorno '{EnvVar}' no configurada para '{Email}'. " +
            "Se generó una contraseña aleatoria — ver salida de consola (stderr). " +
            "Cámbiala inmediatamente después del primer inicio de sesión.",
            envVarName, userEmail);
        Console.Error.WriteLine(
            $"[MEDFLOW SEED] Contraseña inicial para '{userEmail}': {password}  " +
            $"(env: {envVarName}) — Cámbiala inmediatamente.");
        return password;
    }

    private static string GenerateSecurePassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%^&*";
        const string all = upper + lower + digits + special;

        var bytes = new byte[20];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        var chars = new char[16];
        chars[0] = upper[bytes[0] % upper.Length];
        chars[1] = lower[bytes[1] % lower.Length];
        chars[2] = digits[bytes[2] % digits.Length];
        chars[3] = special[bytes[3] % special.Length];
        for (int i = 4; i < chars.Length; i++)
            chars[i] = all[bytes[i] % all.Length];

        // Mezclar posiciones para evitar patrón predecible
        rng.GetBytes(bytes);
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = bytes[i] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
