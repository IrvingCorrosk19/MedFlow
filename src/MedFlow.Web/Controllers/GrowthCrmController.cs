using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePermission(PermissionCodes.PatientsView)]
public class GrowthCrmController : Controller
{
    private readonly IPatientService _patients;
    private readonly ITenantContext _tenant;
    private readonly IGrowthCrmAnalyticsService _engagement;

    public GrowthCrmController(
        IPatientService patients,
        ITenantContext tenant,
        IGrowthCrmAnalyticsService engagement)
    {
        _patients = patients;
        _tenant = tenant;
        _engagement = engagement;
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "CRM Crecimiento";
        ViewData["PageSubtitle"] = "Segmentos y rutas rápidas para retención y recurrencia";
        ViewData["Breadcrumb"] =
            "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action("Index", "Dashboard") + "\">Mission Control</a></li><li class=\"breadcrumb-item active\">CRM Crecimiento</li>";

        return View();
    }

    public async Task<IActionResult> Segments(CancellationToken cancellationToken)
    {
        if (!_tenant.TenantId.HasValue)
            return NotFound();

        ViewData["Title"] = "Segmentos CRM";
        ViewData["PageSubtitle"] = "Conteos en tiempo real del directorio (v1)";
        ViewData["Breadcrumb"] =
            "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action("Index", "Dashboard") + "\">Mission Control</a></li>" +
            "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action("Index", "GrowthCrm") + "\">CRM Crecimiento</a></li>" +
            "<li class=\"breadcrumb-item active\">Segmentos</li>";

        var all = await _patients.GetPagedAsync(null, null, 1, 1, cancellationToken: cancellationToken);
        var active = await _patients.GetPagedAsync(null, true, 1, 1, cancellationToken: cancellationToken);
        var inactive = await _patients.GetPagedAsync(null, false, 1, 1, cancellationToken: cancellationToken);

        ViewBag.CountAll = all.TotalCount;
        ViewBag.CountActive = active.TotalCount;
        ViewBag.CountInactive = inactive.TotalCount;

        var top = await _engagement.GetTopPatientsByAppointmentVolumeAsync(_tenant.TenantId.Value, 365, 10, cancellationToken);
        ViewBag.TopPatients = top;

        return View();
    }
}
