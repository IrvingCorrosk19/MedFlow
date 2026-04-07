using MedFlow.Application.Interfaces;
using MedFlow.Application.Onboarding;
using MedFlow.Application.Options;
using MedFlow.Application.Security;
using MedFlow.Domain.Entities;
using MedFlow.Web.Authorization;
using MedFlow.Web.Models.Accounting;
using MedFlow.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePermission(PermissionCodes.SettingsManage)]
public class SettingsController : Controller
{
    private readonly IClinicSettingsService _clinicSettings;
    private readonly ITenantContext _tenantContext;
    private readonly IOptions<AccountMappingOptions> _accountMapping;
    private readonly IApplicationDbContext _context;
    private readonly IAccountService _accountService;

    public SettingsController(
        IClinicSettingsService clinicSettings,
        ITenantContext tenantContext,
        IOptions<AccountMappingOptions> accountMapping,
        IApplicationDbContext context,
        IAccountService accountService)
    {
        _clinicSettings = clinicSettings;
        _tenantContext = tenantContext;
        _accountMapping = accountMapping;
        _context = context;
        _accountService = accountService;
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "Configuración";
        ViewData["PageSubtitle"] = "Parámetros generales de la clínica";
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Clinic(CancellationToken ct)
    {
        if (!_tenantContext.TenantId.HasValue) return NotFound();
        var dto = await _clinicSettings.GetAsync(_tenantContext.TenantId.Value, ct);
        var vm = new ClinicSettingsViewModel
        {
            Name               = dto.Name,
            LegalName          = dto.LegalName,
            TaxId              = dto.TaxId,
            Email              = dto.Email,
            Phone              = dto.Phone,
            Address            = dto.Address,
            LogoUrl            = dto.LogoUrl,
            PrimaryColor       = dto.PrimaryColor ?? "#1a5f7a",
            SecondaryColor     = dto.SecondaryColor ?? "#0d9488",
            Timezone           = dto.Timezone,
            Currency           = dto.Currency ?? "USD",
            BusinessHoursStart = dto.BusinessHoursStart ?? "08:00",
            BusinessHoursEnd   = dto.BusinessHoursEnd ?? "17:00",
        };
        ViewData["Title"] = "Configuración de la clínica";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clinic(ClinicSettingsViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Configuración de la clínica";
            return View(vm);
        }
        if (!_tenantContext.TenantId.HasValue) return NotFound();

        var dto = new ClinicSettingsDto(
            vm.Name, vm.LegalName, vm.TaxId, vm.Email, vm.Phone,
            vm.Address, vm.LogoUrl, vm.PrimaryColor, vm.SecondaryColor,
            vm.Timezone, vm.Currency, vm.BusinessHoursStart, vm.BusinessHoursEnd);

        await _clinicSettings.UpdateAsync(_tenantContext.TenantId.Value, dto, ct);
        TempData["Success"] = "Configuración de la clínica guardada correctamente.";
        return RedirectToAction(nameof(Clinic));
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> AccountMapping(CancellationToken ct)
    {
        if (!_tenantContext.TenantId.HasValue) return NotFound();
        var tenantId = _tenantContext.TenantId.Value;

        // Load per-tenant overrides from TenantSettings (accounting.map.* keys)
        var keys = new[]
        {
            TenantSettingKeys.AccountingCashCode,
            TenantSettingKeys.AccountingReceivablesCode,
            TenantSettingKeys.AccountingRevenueCode,
            TenantSettingKeys.AccountingTaxPayableCode,
            TenantSettingKeys.AccountingRetainedEarningsCode,
        };

        var stored = await _context.TenantSettings
            .Where(s => s.TenantId == tenantId && keys.Contains(s.SettingKey))
            .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue ?? "", ct);

        string Resolve(string key, string fallback) =>
            stored.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;

        var opts = _accountMapping.Value;
        var vm = new AccountMappingSettingsViewModel
        {
            CashAccountCode            = Resolve(TenantSettingKeys.AccountingCashCode,             opts.CashAccountCode),
            ReceivablesAccountCode     = Resolve(TenantSettingKeys.AccountingReceivablesCode,      opts.ReceivablesAccountCode),
            RevenueAccountCode         = Resolve(TenantSettingKeys.AccountingRevenueCode,          opts.RevenueAccountCode),
            TaxPayableAccountCode      = Resolve(TenantSettingKeys.AccountingTaxPayableCode,       opts.TaxPayableAccountCode),
            RetainedEarningsAccountCode= Resolve(TenantSettingKeys.AccountingRetainedEarningsCode, opts.RetainedEarningsAccountCode),
        };

        ViewBag.Accounts = await _accountService.GetAllAsync(tenantId, ct);
        ViewData["Title"] = "Mapeo de Cuentas Contables";
        return View("Accounting", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> SaveAccountMapping(AccountMappingSettingsViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            if (_tenantContext.TenantId.HasValue)
                ViewBag.Accounts = await _accountService.GetAllAsync(_tenantContext.TenantId.Value, ct);
            ViewData["Title"] = "Mapeo de Cuentas Contables";
            return View("Accounting", vm);
        }
        if (!_tenantContext.TenantId.HasValue) return NotFound();
        var tenantId = _tenantContext.TenantId.Value;

        var pairs = new[]
        {
            (TenantSettingKeys.AccountingCashCode,             vm.CashAccountCode),
            (TenantSettingKeys.AccountingReceivablesCode,      vm.ReceivablesAccountCode),
            (TenantSettingKeys.AccountingRevenueCode,          vm.RevenueAccountCode),
            (TenantSettingKeys.AccountingTaxPayableCode,       vm.TaxPayableAccountCode),
            (TenantSettingKeys.AccountingRetainedEarningsCode, vm.RetainedEarningsAccountCode),
        };

        var keyList = pairs.Select(p => p.Item1).ToList();
        var existing = await _context.TenantSettings
            .Where(s => s.TenantId == tenantId && keyList.Contains(s.SettingKey))
            .ToDictionaryAsync(s => s.SettingKey, ct);

        foreach (var (key, value) in pairs)
        {
            if (existing.TryGetValue(key, out var s))
            {
                s.SettingValue = value;
                s.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.TenantSettings.Add(new TenantSettings
                {
                    TenantId = tenantId,
                    SettingKey = key,
                    SettingValue = value,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync(ct);
        TempData["Success"] = "Configuración de cuentas guardada correctamente.";
        return RedirectToAction(nameof(AccountMapping));
    }
}
