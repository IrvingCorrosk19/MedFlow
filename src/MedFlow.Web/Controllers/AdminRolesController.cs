using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using MedFlow.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
[Authorize(Roles = "Admin,SuperAdmin")]
[RequirePermission(PermissionCodes.RolesManage)]
public class AdminRolesController : Controller
{
    private readonly IRoleAdminService _roles;
    private readonly IPermissionCatalogService _permissions;
    private readonly IPermissionChecker _perm;
    private readonly UserManager<MedFlow.Infrastructure.Identity.ApplicationUser> _userManager;

    public AdminRolesController(
        IRoleAdminService roles,
        IPermissionCatalogService permissions,
        IPermissionChecker perm,
        UserManager<MedFlow.Infrastructure.Identity.ApplicationUser> userManager)
    {
        _roles = roles;
        _permissions = permissions;
        _perm = perm;
        _userManager = userManager;
    }

    private async Task<IActionResult?> EnsureAllowedAsync(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Forbid();

        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        var isSuperAdmin = await _userManager.IsInRoleAsync(user, "SuperAdmin");
        if (!isAdmin && !isSuperAdmin)
            return Forbid();

        var userId = user.Id;
        if (string.IsNullOrWhiteSpace(userId))
            return Forbid();
        if (!await _perm.UserHasPermissionAsync(userId, PermissionCodes.RolesManage, cancellationToken))
            return Forbid();
        return null;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var gate = await EnsureAllowedAsync(cancellationToken);
        if (gate != null) return gate;

        ViewData["Title"] = "Roles";
        ViewData["PageSubtitle"] = "Perfiles de acceso";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Roles</li>";

        var list = await _roles.GetAllAsync(cancellationToken);
        return View(list);
    }

    public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
    {
        var r = await _roles.GetByIdAsync(id, cancellationToken);
        if (r == null) return NotFound();

        ViewData["Title"] = r.Name;
        ViewData["PageSubtitle"] = r.Description ?? "Detalle del rol";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Roles</a></li><li class=\"breadcrumb-item active\">Detalle</li>";

        return View(r);
    }

    public IActionResult Create()
    {
        ViewData["Title"] = "Nuevo rol";
        ViewData["PageSubtitle"] = "Definir perfil";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Roles</a></li><li class=\"breadcrumb-item active\">Nuevo</li>";

        return View(new RoleFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Nuevo rol";
            ViewData["PageSubtitle"] = "Definir perfil";
            ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Roles</a></li><li class=\"breadcrumb-item active\">Nuevo</li>";
            return View(model);
        }

        var (ok, err) = await _roles.CreateAsync(new RoleAdminCreateDto
        {
            Name = model.Name,
            Description = model.Description
        }, cancellationToken);

        if (!ok)
        {
            ModelState.AddModelError(string.Empty, err ?? "No se pudo crear el rol.");
            ViewData["Title"] = "Nuevo rol";
            ViewData["PageSubtitle"] = "Definir perfil";
            ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Roles</a></li><li class=\"breadcrumb-item active\">Nuevo</li>";
            return View(model);
        }

        TempData["Success"] = "Rol creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken)
    {
        var r = await _roles.GetByIdAsync(id, cancellationToken);
        if (r == null) return NotFound();

        ViewData["Title"] = "Editar rol";
        ViewData["PageSubtitle"] = r.Name;
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Roles</a></li><li class=\"breadcrumb-item active\">Editar</li>";

        return View(new RoleFormViewModel
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            IsActive = r.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, RoleFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Editar rol";
            ViewData["PageSubtitle"] = model.Name;
            ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Roles</a></li><li class=\"breadcrumb-item active\">Editar</li>";
            return View(model);
        }

        var (ok, err) = await _roles.UpdateAsync(new RoleAdminUpdateDto
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            IsActive = model.IsActive
        }, cancellationToken);

        if (!ok)
        {
            ModelState.AddModelError(string.Empty, err ?? "No se pudo actualizar el rol.");
            ViewData["Title"] = "Editar rol";
            ViewData["PageSubtitle"] = model.Name;
            ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Roles</a></li><li class=\"breadcrumb-item active\">Editar</li>";
            return View(model);
        }

        TempData["Success"] = "Rol actualizado correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var (ok, err) = await _roles.DeleteAsync(id, cancellationToken);
        if (!ok)
        {
            TempData["Error"] = err ?? "No se pudo eliminar el rol.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Rol eliminado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Permissions(string id, CancellationToken cancellationToken)
    {
        var r = await _roles.GetByIdAsync(id, cancellationToken);
        if (r == null) return NotFound();

        var all = await _permissions.GetAllAsync(cancellationToken);
        var selected = r.PermissionIds.ToHashSet();
        var grouped = all.GroupBy(p => p.Module).OrderBy(g => g.Key);

        var vm = new RolePermissionsViewModel
        {
            RoleId = r.Id,
            RoleName = r.Name,
            Modules = grouped.Select(g => new PermissionModuleGroupVm
            {
                Module = g.Key,
                Items = g.Select(p => new PermissionCheckboxVm
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    Selected = selected.Contains(p.Id)
                }).ToList()
            }).ToList()
        };

        ViewData["Title"] = "Permisos del rol";
        ViewData["PageSubtitle"] = r.Name;
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Roles</a></li><li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Details), new { id }) + "\">Detalle</a></li><li class=\"breadcrumb-item active\">Permisos</li>";

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Permissions(string id, List<Guid> permissionIds, CancellationToken cancellationToken)
    {
        permissionIds ??= new List<Guid>();
        var (ok, err) = await _roles.SetPermissionsAsync(id, permissionIds, cancellationToken);
        if (!ok)
        {
            TempData["Error"] = err ?? "No se pudieron guardar los permisos.";
            return RedirectToAction(nameof(Permissions), new { id });
        }

        TempData["Success"] = "Permisos actualizados.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
