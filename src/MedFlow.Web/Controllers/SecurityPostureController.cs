using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

/// <summary>Checklist de postura de seguridad (lectura) para administradores de clínica.</summary>
[Authorize]
[RequirePermission(PermissionCodes.SettingsManage)]
public class SecurityPostureController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Postura de seguridad";
        ViewData["PageSubtitle"] = "Controles activos y buenas prácticas";
        ViewData["Breadcrumb"] =
            "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action("Index", "Dashboard") + "\">Mission Control</a></li><li class=\"breadcrumb-item active\">Seguridad</li>";

        return View();
    }
}
