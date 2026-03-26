using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePermission(PermissionCodes.PermissionsView)]
public class PermissionsController : Controller
{
    private readonly IPermissionCatalogService _catalog;

    public PermissionsController(IPermissionCatalogService catalog)
    {
        _catalog = catalog;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Permisos";
        ViewData["PageSubtitle"] = "Catálogo de permisos del sistema";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Permisos</li>";

        var list = await _catalog.GetAllAsync(cancellationToken);
        return View(list);
    }
}
