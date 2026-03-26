using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePermission(PermissionCodes.SettingsManage)]
public class SettingsController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Configuración";
        ViewData["PageSubtitle"] = "Parámetros generales de la clínica";
        return View();
    }
}
