using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.AI;
using MedFlow.Application.Interfaces.Workflow;
using MedFlow.Application.Options;
using MedFlow.Infrastructure.Billing;
using MedFlow.Infrastructure.Identity;
using MedFlow.Infrastructure.Notifications;
using MedFlow.Infrastructure.Persistence;
using MedFlow.Infrastructure.Services;
using MedFlow.Infrastructure.Tenancy;
using MedFlow.Infrastructure.AI;
using MedFlow.Infrastructure.Workflow;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Linq;

namespace MedFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=medflow;Username=postgres;Password=postgres";

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContext>();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(3);
            }));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IEventLogService, EventLogService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IMedicalRecordService, MedicalRecordService>();
        services.AddScoped<IBillingInvoiceService, BillingInvoiceService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<ICashMovementService, CashMovementService>();
        services.AddScoped<IEventLogQueryService, EventLogQueryService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IPermissionChecker, PermissionChecker>();
        services.AddScoped<IPermissionCatalogService, PermissionCatalogService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IRoleAdminService, RoleAdminService>();
        services.AddScoped<IExecutiveAnalyticsService, ExecutiveAnalyticsService>();
        services.AddScoped<ISubscriptionLimitService, SubscriptionLimitService>();
        services.AddScoped<IPlanFeatureService, PlanFeatureService>();
        services.AddScoped<ISaasTenantAdminService, SaasTenantAdminService>();
        services.AddScoped<ISubscriptionPlanAdminService, SubscriptionPlanAdminService>();
        services.AddScoped<ISaasSubscriptionQueryService, SaasSubscriptionQueryService>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddScoped<IBillingProvider, StripeBillingProvider>();
        services.AddScoped<ISaasBillingService, SaasBillingService>();
        services.AddScoped<IStripeWebhookProcessor, StripeWebhookProcessor>();
        services.AddScoped<ISaasBillingQueryService, SaasBillingQueryService>();

        services.AddHttpClient();

        services.AddSingleton<IPayloadTemplateEngine, PayloadTemplateEngine>();
        services.AddScoped<IWorkflowDispatcher, N8nWorkflowDispatcher>();
        services.AddScoped<IWorkflowTriggerService, WorkflowTriggerService>();
        services.AddScoped<IWorkflowDefinitionService, WorkflowDefinitionService>();
        services.AddScoped<IWorkflowExecutionService, WorkflowExecutionService>();
        services.AddScoped<IWorkflowTestService, WorkflowTestService>();
        services.AddHostedService<WorkflowProcessorService>();

        services.AddScoped<IClinicSettingsService, Services.ClinicSettingsService>();

        // Contabilidad
        services.AddScoped<IAccountService, Services.AccountService>();
        services.AddScoped<IFiscalPeriodService, Services.FiscalPeriodService>();
        services.AddScoped<IAccountingExportService, Services.AccountingExportService>();
        services.AddScoped<IJournalEntryService, Services.JournalEntryService>();
        services.AddScoped<ILedgerService, Services.LedgerService>();
        services.AddScoped<ITaxRateService, Services.TaxRateService>();
        services.AddScoped<IBankAccountService, Services.BankAccountService>();
        services.AddScoped<IAISettingsService, AISettingsService>();
        services.AddScoped<INoShowRiskService, NoShowRiskService>();
        services.AddScoped<IPaymentRiskService, PaymentRiskService>();
        services.AddScoped<IPatientEngagementService, PatientEngagementService>();
        services.AddScoped<IRecommendationEngine, RecommendationEngine>();
        services.AddScoped<IAIInsightService, AIInsightService>();
        services.AddScoped<IAIInsightProcessorService, AIInsightProcessorService>();
        services.AddScoped<IOperationalCopilotService, OperationalCopilotService>();
        services.AddScoped<IOperationalSummaryService, OperationalSummaryService>();
        services.AddScoped<Application.Interfaces.AI.Providers.IAIModelProvider, AI.Providers.NullAIModelProvider>();
        services.AddScoped<Application.Interfaces.AI.Providers.IInferenceProvider, AI.Providers.RuleBasedInferenceProvider>();
        services.AddScoped<Application.Interfaces.AI.Providers.IRiskScoringProvider, AI.Providers.RuleBasedRiskScoringProvider>();
        services.AddHostedService<AIInsightProcessorBackgroundService>();

        services.AddScoped<ISnapshotAggregationService, Analytics.SnapshotAggregationService>();
        services.AddScoped<IAnalyticsAggregationService>(sp => (IAnalyticsAggregationService)sp.GetRequiredService<ISnapshotAggregationService>());
        services.AddScoped<Analytics.AnalyticsSnapshotProcessorService>();
        services.AddScoped<IAdvancedAnalyticsService, Analytics.AdvancedAnalyticsService>();
        services.AddScoped<IHistoricalAnalyticsService, Analytics.HistoricalAnalyticsService>();
        services.AddScoped<IPeriodComparisonService, Analytics.PeriodComparisonService>();
        services.AddScoped<ITenantHealthService, Analytics.TenantHealthService>();
        services.AddScoped<IBenchmarkingService, Analytics.BenchmarkingService>();
        services.AddScoped<IAnalyticsRebuildService, Analytics.AnalyticsRebuildService>();
        services.AddScoped<IAnalyticsSettingsService, Analytics.AnalyticsSettingsService>();
        services.AddHostedService<Analytics.AnalyticsSnapshotBackgroundService>();
        services.AddHostedService<Notifications.AppointmentReminderBackgroundService>();

        services.Configure<ResendOptions>(configuration.GetSection(ResendOptions.SectionName));
        services.Configure<Resend.ResendClientOptions>(o =>
        {
            o.ApiToken = configuration[$"{ResendOptions.SectionName}:ApiKey"]
                ?? Environment.GetEnvironmentVariable("RESEND_API_KEY")
                ?? "";
        });
        services.AddHttpClient<Resend.ResendClient>();
        services.AddTransient<Resend.IResend, Resend.ResendClient>();
        services.AddScoped<IEmailSender, ResendEmailSender>();
        services.AddScoped<IWebhookSender, HttpClientWebhookSender>();
        services.AddScoped<INotificationDispatchService, NotificationDispatchService>();
        services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
        services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
        services.AddScoped<IPatientPortalService, PatientPortalService>();
        services.AddScoped<IPrescriptionService, Services.PrescriptionService>();
        services.AddScoped<IPatientPortalEnableService, PatientPortalEnableService>();
        services.AddScoped<IPushDeviceTokenService, PushDeviceTokenService>();
        services.AddScoped<ITenantGuardService, TenantGuardService>();
        services.AddScoped<IWorkerHeartbeatService, WorkerHeartbeatService>();
        services.AddScoped<IWebhookEventQueryService, WebhookEventQueryService>();
        services.AddScoped<Startup.IStartupValidator, Startup.StartupValidator>();

        services.Configure<MedFlow.Application.Options.AccountMappingOptions>(configuration.GetSection(MedFlow.Application.Options.AccountMappingOptions.SectionName));
        services.Configure<MedFlow.Application.Options.JwtOptions>(configuration.GetSection(MedFlow.Application.Options.JwtOptions.SectionName));
        services.AddScoped<MedFlow.Application.Interfaces.IJwtTokenService, JwtTokenService>();
        services.AddScoped<MedFlow.Application.Interfaces.IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<MedFlow.Application.Interfaces.IMobileAuthService, MobileAuthService>();
        services.AddScoped<MedFlow.Application.Interfaces.ITenantStaffAuthService, TenantStaffAuthService>();

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // Contraseñas robustas requeridas para un sistema de salud.
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 10;
            options.Password.RequiredUniqueChars = 4;

            // Emails únicos para evitar ambigüedad de identidad.
            options.User.RequireUniqueEmail = true;

            // Bloqueo de cuenta explícito: 5 intentos fallidos → 15 minutos de bloqueo.
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.SlidingExpiration = true;
        });

        var oldFactory = services.FirstOrDefault(s => s.ServiceType == typeof(IUserClaimsPrincipalFactory<ApplicationUser>));
        if (oldFactory != null)
            services.Remove(oldFactory);
        services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, MedFlowUserClaimsPrincipalFactory>();

        services.AddScoped<SignInManager<ApplicationUser>, MedFlowSignInManager>();

        // OpenTelemetry
        var obsOptions = configuration.GetSection("Observability").Get<ObservabilityOptions>() ?? new();
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                            .AddService(obsOptions.ServiceName))
                    .AddAspNetCoreInstrumentation(o => { o.RecordException = true; })
                    .AddHttpClientInstrumentation()
                    .AddSource("MedFlow.Infrastructure");
                if (!string.IsNullOrWhiteSpace(obsOptions.OtlpEndpoint))
                    builder.AddOtlpExporter(o => o.Endpoint = new Uri(obsOptions.OtlpEndpoint));
                if (obsOptions.EnableConsoleExporter)
                    builder.AddConsoleExporter();
            })
            .WithMetrics(builder =>
            {
                builder
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                            .AddService(obsOptions.ServiceName))
                    .AddAspNetCoreInstrumentation()
                    .AddMeter("MedFlow.Infrastructure");
                if (!string.IsNullOrWhiteSpace(obsOptions.OtlpEndpoint))
                    builder.AddOtlpExporter(o => o.Endpoint = new Uri(obsOptions.OtlpEndpoint));
                if (obsOptions.EnableConsoleExporter)
                    builder.AddConsoleExporter();
            });

        return services;
    }
}
