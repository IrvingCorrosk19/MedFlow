using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using MedFlow.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePermission(PermissionCodes.SettingsManage)]
public class SettingsController : Controller
{
    private readonly IClinicSettingsService _clinicSettings;
    private readonly ITenantContext _tenantContext;

    public SettingsController(IClinicSettingsService clinicSettings, ITenantContext tenantContext)
    {
        _clinicSettings = clinicSettings;
        _tenantContext = tenantContext;
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
}
