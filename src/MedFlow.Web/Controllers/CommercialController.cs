using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
public class CommercialController : Controller
{
    public IActionResult Blocked(string? reason)
    {
        ViewData["Title"] = "Acceso restringido";
        ViewData["PageSubtitle"] = "Estado comercial de la clínica";
        ViewBag.Reason = reason;
        return View();
    }

    public IActionResult UpgradeRequired(string? feature)
    {
        ViewData["Title"] = "Plan insuficiente";
        ViewData["PageSubtitle"] = "La función solicitada no está incluida en su suscripción actual.";
        ViewBag.Feature = feature;
        return View();
    }
}
