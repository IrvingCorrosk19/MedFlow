using System.Text;
using MedFlow.Application.Interfaces;
using MedFlow.Application.Onboarding;
using MedFlow.Application.Options;
using MedFlow.Web.Infrastructure;
using MedFlow.Web.Models.Onboarding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MedFlow.Web.Controllers;

[AllowAnonymous]
public sealed class OnboardingController : Controller
{
    private readonly ITenantProvisioningService _provisioning;
    private readonly ISubscriptionPlanAdminService _plans;
    private readonly IDataProtectionProvider _dataProtection;
    private readonly IOptions<OnboardingOptions> _options;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(
        ITenantProvisioningService provisioning,
        ISubscriptionPlanAdminService plans,
        IDataProtectionProvider dataProtection,
        IOptions<OnboardingOptions> options,
        IConfiguration configuration,
        ILogger<OnboardingController> logger)
    {
        _provisioning = provisioning;
        _plans = plans;
        _dataProtection = dataProtection;
        _options = options;
        _configuration = configuration;
        _logger = logger;
    }

    private IDataProtector PasswordProtector => _dataProtection.CreateProtector("MedFlow.Onboarding", "Password", "v1");

    [HttpGet]
    public IActionResult Index()
    {
        if (!EnsureEnabled(out var denied)) return denied!;
        return RedirectToAction(nameof(Step1));
    }

    [HttpGet]
    public async Task<IActionResult> Step1(CancellationToken cancellationToken)
    {
        if (!EnsureEnabled(out var denied)) return denied!;
        await HttpContext.Session.LoadAsync(cancellationToken).ConfigureAwait(false);
        var s = HttpContext.Session.GetOnboardingState();
        var vm = new OnboardingStep1Vm
        {
            ClinicName = s?.ClinicName ?? "",
            Code = s?.Code ?? "",
            ClinicEmail = s?.ClinicEmail,
            Phone = s?.Phone,
            Address = s?.Address
        };
        ViewData["Title"] = "Datos de la clínica";
        ViewData["OnboardingStep"] = 1;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Step1(OnboardingStep1Vm model, CancellationToken cancellationToken)
    {
        if (!EnsureEnabled(out var denied)) return denied!;
        await HttpContext.Session.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Datos de la clínica";
            ViewData["OnboardingStep"] = 1;
            return View(model);
        }

        var s = HttpContext.Session.GetOnboardingState() ?? new OnboardingSessionState();
        s.ClinicName = model.ClinicName.Trim();
        s.Code = model.Code.Trim().ToLowerInvariant();
        s.ClinicEmail = string.IsNullOrWhiteSpace(model.ClinicEmail) ? null : model.ClinicEmail.Trim();
        s.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        s.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
        HttpContext.Session.SetOnboardingState(s);
        return RedirectToAction(nameof(Step2));
    }

    [HttpGet]
    public async Task<IActionResult> Step2(CancellationToken cancellationToken)
    {
        if (!EnsureEnabled(out var denied)) return denied!;
        await HttpContext.Session.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (HttpContext.Session.GetOnboardingState() == null)
            return RedirectToAction(nameof(Step1));

        var all = await _plans.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var active = all.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToList();
        var cards = new List<OnboardingPlanCardVm>();
        foreach (var p in active)
        {
            var detail = await _plans.GetForEditAsync(p.Id, cancellationToken).ConfigureAwait(false);
            if (detail == null) continue;
            cards.Add(OnboardingPlanCardVm.FromEdit(p.Id, detail));
        }

        var s = HttpContext.Session.GetOnboardingState()!;
        var vm = new OnboardingStep2Vm
        {
            SubscriptionPlanId = s.SubscriptionPlanId,
            StartWithTrial = s.StartWithTrial
        };

        ViewData["Title"] = "Plan SaaS";
        ViewData["OnboardingStep"] = 2;
        ViewBag.PlanCards = cards;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Step2(OnboardingStep2Vm model, CancellationToken cancellationToken)
    {
        if (!EnsureEnabled(out var denied)) return denied!;
        await HttpContext.Session.LoadAsync(cancellationToken).ConfigureAwait(false);
        var s = HttpContext.Session.GetOnboardingState();
        if (s == null)
            return RedirectToAction(nameof(Step1));

        var detail = model.SubscriptionPlanId.HasValue
            ? await _plans.GetForEditAsync(model.SubscriptionPlanId.Value, cancellationToken).ConfigureAwait(false)
            : null;

        if (detail == null || !detail.IsActive)
        {
            ModelState.AddModelError(nameof(model.SubscriptionPlanId), "Seleccione un plan válido.");
        }
        else if (model.StartWithTrial && detail.TrialDays <= 0)
        {
            model.StartWithTrial = false;
        }

        if (!ModelState.IsValid)
        {
            var all = await _plans.GetAllAsync(cancellationToken).ConfigureAwait(false);
            var active = all.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToList();
            var cards = new List<OnboardingPlanCardVm>();
            foreach (var p in active)
            {
                var d = await _plans.GetForEditAsync(p.Id, cancellationToken).ConfigureAwait(false);
                if (d == null) continue;
                cards.Add(OnboardingPlanCardVm.FromEdit(p.Id, d));
            }

            ViewBag.PlanCards = cards;
            ViewData["Title"] = "Plan SaaS";
            ViewData["OnboardingStep"] = 2;
            return View(model);
        }

        s.SubscriptionPlanId = model.SubscriptionPlanId;
        s.StartWithTrial = model.StartWithTrial;
        s.SubscriptionPlanName = detail!.Name;
        s.SubscriptionPlanCode = detail.Code;
        HttpContext.Session.SetOnboardingState(s);
        return RedirectToAction(nameof(Step3));
    }

    [HttpGet]
    public async Task<IActionResult> Step3(CancellationToken cancellationToken)
    {
        if (!EnsureEnabled(out var denied)) return denied!;
        await HttpContext.Session.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (HttpContext.Session.GetOnboardingState()?.SubscriptionPlanId == null)
            return RedirectToAction(nameof(Step1));

        var s = HttpContext.Session.GetOnboardingState()!;
        var vm = new OnboardingStep3Vm
        {
            AdminFirstName = s.AdminFirstName,
            AdminLastName = s.AdminLastName,
            AdminEmail = s.AdminEmail
        };
        ViewData["Title"] = "Administrador";
        ViewData["OnboardingStep"] = 3;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Step3(OnboardingStep3Vm model, CancellationToken cancellationToken)
    {
        if (!EnsureEnabled(out var denied)) return denied!;
        await HttpContext.Session.LoadAsync(cancellationToken).ConfigureAwait(false);
        var s = HttpContext.Session.GetOnboardingState();
        if (s?.SubscriptionPlanId == null)
            return RedirectToAction(nameof(Step1));

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Administrador";
            ViewData["OnboardingStep"] = 3;
            return View(model);
        }

        try
        {
            var bytes = Encoding.UTF8.GetBytes(model.Password);
            s.ProtectedPasswordPayload = Convert.ToBase64String(PasswordProtector.Protect(bytes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo proteger la contraseña de onboarding");
            ModelState.AddModelError(string.Empty, "No se pudo guardar el paso de forma segura. Intente de nuevo.");
            ViewData["Title"] = "Administrador";
            ViewData["OnboardingStep"] = 3;
            return View(model);
        }

        s.AdminFirstName = model.AdminFirstName.Trim();
        s.AdminLastName = model.AdminLastName.Trim();
        s.AdminEmail = model.AdminEmail.Trim();
        HttpContext.Session.SetOnboardingState(s);
        return RedirectToAction(nameof(Step4));
    }

    [HttpGet]
    public async Task<IActionResult> Step4(CancellationToken cancellationToken)
    {
        if (!EnsureEnabled(out var denied)) return denied!;
        await HttpContext.Session.LoadAsync(cancellationToken).ConfigureAwait(false);
        var s = HttpContext.Session.GetOnboardingState();
        if (s == null || string.IsNullOrEmpty(s.ProtectedPasswordPayload))
            return RedirectToAction(nameof(Step1));

        var vm = new OnboardingStep4Vm
        {
            TimeZoneId = s.TimeZoneId,
            DateFormat = s.DateFormat,
            Currency = s.Currency,
            Language = s.Language
        };
        ViewData["Title"] = "Configuración inicial";
        ViewData["OnboardingStep"] = 4;
        ViewBag.TimeZones = TimeZoneChoices;
        ViewBag.DateFormats = DateFormatChoices;
        ViewBag.Currencies = CurrencyChoices;
        ViewBag.Languages = LanguageChoices;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Step4(OnboardingStep4Vm model, CancellationToken cancellationToken)
    {
        if (!EnsureEnabled(out var denied)) return denied!;
        await HttpContext.Session.LoadAsync(cancellationToken).ConfigureAwait(false);
        var s = HttpContext.Session.GetOnboardingState();
        if (s == null || string.IsNullOrEmpty(s.ProtectedPasswordPayload))
            return RedirectToAction(nameof(Step1));

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Configuración inicial";
            ViewData["OnboardingStep"] = 4;
            ViewBag.TimeZones = TimeZoneChoices;
            ViewBag.DateFormats = DateFormatChoices;
            ViewBag.Currencies = CurrencyChoices;
            ViewBag.Languages = LanguageChoices;
            return View(model);
        }

        s.TimeZoneId = model.TimeZoneId.Trim();
        s.DateFormat = model.DateFormat.Trim();
        s.Currency = model.Currency.Trim();
        s.Language = model.Language.Trim();
        HttpContext.Session.SetOnboardingState(s);
        return RedirectToAction(nameof(Step5));
    }

    [HttpGet]
    public async Task<IActionResult> Step5(CancellationToken cancellationToken)
    {
        if (!EnsureEnabled(out var denied)) return denied!;
        await HttpContext.Session.LoadAsync(cancellationToken).ConfigureAwait(false);
        var s = HttpContext.Session.GetOnboardingState();
        if (s == null || s.SubscriptionPlanId == null || string.IsNullOrEmpty(s.ProtectedPasswordPayload))
            return RedirectToAction(nameof(Step1));

        ViewData["Title"] = "Confirmación";
        ViewData["OnboardingStep"] = 5;
        return View(s);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(CancellationToken cancellationToken)
    {
        if (!EnsureEnabled(out var denied)) return denied!;
        await HttpContext.Session.LoadAsync(cancellationToken).ConfigureAwait(false);
        var s = HttpContext.Session.GetOnboardingState();
        if (s == null || s.SubscriptionPlanId == null || string.IsNullOrEmpty(s.ProtectedPasswordPayload))
        {
            TempData["OnboardingError"] = "La sesión de onboarding expiró o es inválida. Comience de nuevo.";
            return RedirectToAction(nameof(Step1));
        }

        string password;
        try
        {
            var raw = Convert.FromBase64String(s.ProtectedPasswordPayload);
            password = Encoding.UTF8.GetString(PasswordProtector.Unprotect(raw));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Payload de contraseña de onboarding inválido o corrupto");
            TempData["OnboardingError"] = "No se pudo recuperar la contraseña del paso anterior. Vuelva al paso 3.";
            return RedirectToAction(nameof(Step3));
        }

        var request = new TenantProvisioningRequest
        {
            ClinicName = s.ClinicName,
            Code = s.Code,
            ClinicEmail = s.ClinicEmail,
            Phone = s.Phone,
            Address = s.Address,
            SubscriptionPlanId = s.SubscriptionPlanId.Value,
            StartWithTrial = s.StartWithTrial,
            AdminFirstName = s.AdminFirstName,
            AdminLastName = s.AdminLastName,
            AdminEmail = s.AdminEmail,
            AdminPassword = password,
            TimeZoneId = s.TimeZoneId,
            DateFormat = s.DateFormat,
            Currency = s.Currency,
            Language = s.Language
        };

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _provisioning.ProvisionAsync(request, ip, cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            TempData["OnboardingError"] = string.Join(" ", result.Errors);
            return RedirectToAction(nameof(Step5));
        }

        HttpContext.Session.ClearOnboardingState();

        TempData["OnboardingTenantCode"] = result.TenantCode;
        TempData["OnboardingAdminEmail"] = s.AdminEmail;
        return RedirectToAction(nameof(Success), new { code = result.TenantCode });
    }

    [HttpGet]
    public IActionResult Success(string? code)
    {
        if (!EnsureEnabled(out var denied)) return denied!;
        var tenantCode = TempData["OnboardingTenantCode"] as string ?? code ?? "";
        var adminEmail = TempData["OnboardingAdminEmail"] as string;

        var suffix = _configuration["TenantResolution:HostSuffix"] ?? "";
        string? hostHint = null;
        if (!string.IsNullOrWhiteSpace(tenantCode) && !string.IsNullOrWhiteSpace(suffix))
        {
            var host = Request.Host.Host;
            if (host.Contains('.', StringComparison.Ordinal))
            {
                var root = string.Join('.', host.Split('.', StringSplitOptions.RemoveEmptyEntries).Skip(1));
                hostHint = $"https://{tenantCode}.{root}";
            }
            else
            {
                hostHint = $"https://{tenantCode}.{suffix.TrimStart('.')}";
            }
        }

        var vm = new OnboardingSuccessVm
        {
            TenantCode = tenantCode,
            AdminEmail = adminEmail,
            HostHint = hostHint
        };
        ViewData["Title"] = "Clínica creada";
        ViewData["OnboardingStep"] = 5;
        return View(vm);
    }

    private bool EnsureEnabled(out IActionResult? denied)
    {
        denied = null;
        if (_options.Value.Enabled)
            return true;
        denied = NotFound();
        return false;
    }

    private static IReadOnlyList<string> TimeZoneChoices { get; } =
    [
        "America/Mexico_City",
        "America/Bogota",
        "America/Lima",
        "America/Santiago",
        "America/Argentina/Buenos_Aires",
        "America/New_York",
        "Europe/Madrid",
        "UTC"
    ];

    private static IReadOnlyList<string> DateFormatChoices { get; } = ["dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd"];

    private static IReadOnlyList<string> CurrencyChoices { get; } = ["USD", "MXN", "EUR", "COP", "ARS", "CLP"];

    private static IReadOnlyList<string> LanguageChoices { get; } = ["es", "en"];
}
