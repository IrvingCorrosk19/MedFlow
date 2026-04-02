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
[RequirePermission(PermissionCodes.UsersManage)]
public class AdminUsersController : Controller
{
    private readonly IAdminUserService _users;
    private readonly IRoleAdminService _roles;
    private readonly IPermissionChecker _perm;
    private readonly UserManager<MedFlow.Infrastructure.Identity.ApplicationUser> _userManager;

    public AdminUsersController(
        IAdminUserService users,
        IRoleAdminService roles,
        IPermissionChecker perm,
        UserManager<MedFlow.Infrastructure.Identity.ApplicationUser> userManager)
    {
        _users = users;
        _roles = roles;
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
        if (!await _perm.UserHasPermissionAsync(userId, PermissionCodes.UsersManage, cancellationToken))
            return Forbid();
        return null;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var gate = await EnsureAllowedAsync(cancellationToken);
        if (gate != null) return gate;

        ViewData["Title"] = "Usuarios";
        ViewData["PageSubtitle"] = "Cuentas y acceso al sistema";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Usuarios</li>";

        var list = await _users.GetAllAsync(cancellationToken);
        return View(list);
    }

    public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
    {
        var u = await _users.GetByIdAsync(id, cancellationToken);
        if (u == null) return NotFound();

        ViewData["Title"] = u.FullName ?? u.Email ?? "Usuario";
        ViewData["PageSubtitle"] = u.Email;
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Usuarios</a></li><li class=\"breadcrumb-item active\">Detalle</li>";

        return View(u);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Nuevo usuario";
        ViewData["PageSubtitle"] = "Alta de cuenta";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Usuarios</a></li><li class=\"breadcrumb-item active\">Nuevo</li>";

        var roleNames = await GetRoleNameListAsync(cancellationToken);
        return View(new AdminUserFormViewModel { AllRoleNames = roleNames, IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminUserFormViewModel model, CancellationToken cancellationToken)
    {
        var roleNames = await GetRoleNameListAsync(cancellationToken);
        model.AllRoleNames = roleNames;

        if (string.IsNullOrWhiteSpace(model.Password))
            ModelState.AddModelError(nameof(model.Password), "La contraseña es obligatoria.");

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Nuevo usuario";
            ViewData["PageSubtitle"] = "Alta de cuenta";
            ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Usuarios</a></li><li class=\"breadcrumb-item active\">Nuevo</li>";
            return View(model);
        }

        var dto = new AdminUserCreateDto
        {
            Email = model.Email,
            UserName = model.UserName,
            Password = model.Password ?? "",
            FirstName = model.FirstName,
            MiddleName = model.MiddleName,
            LastName = model.LastName,
            SecondLastName = model.SecondLastName,
            PhoneNumber = model.PhoneNumber,
            IsActive = model.IsActive,
            RoleNames = model.RoleNames ?? new List<string>()
        };

        var (ok, err) = await _users.CreateAsync(dto, cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, err ?? "No se pudo crear el usuario.");
            ViewData["Title"] = "Nuevo usuario";
            ViewData["PageSubtitle"] = "Alta de cuenta";
            ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Usuarios</a></li><li class=\"breadcrumb-item active\">Nuevo</li>";
            return View(model);
        }

        TempData["Success"] = "Usuario creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken)
    {
        var u = await _users.GetByIdAsync(id, cancellationToken);
        if (u == null) return NotFound();

        var roleNames = await GetRoleNameListAsync(cancellationToken);
        var vm = new AdminUserFormViewModel
        {
            Id = u.Id,
            Email = u.Email ?? "",
            UserName = u.UserName,
            FirstName = u.FirstName,
            MiddleName = u.MiddleName,
            LastName = u.LastName,
            SecondLastName = u.SecondLastName,
            PhoneNumber = u.PhoneNumber,
            IsActive = u.IsActive,
            IsLocked = u.IsLocked,
            RoleNames = u.Roles.ToList(),
            AllRoleNames = roleNames
        };

        ViewData["Title"] = "Editar usuario";
        ViewData["PageSubtitle"] = u.Email;
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Usuarios</a></li><li class=\"breadcrumb-item active\">Editar</li>";

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, AdminUserFormViewModel model, CancellationToken cancellationToken)
    {
        var roleNames = await GetRoleNameListAsync(cancellationToken);
        model.AllRoleNames = roleNames;

        if (id != model.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Editar usuario";
            ViewData["PageSubtitle"] = model.Email;
            ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Usuarios</a></li><li class=\"breadcrumb-item active\">Editar</li>";
            return View(model);
        }

        var dto = new AdminUserUpdateDto
        {
            Id = model.Id,
            Email = model.Email,
            UserName = model.UserName,
            FirstName = model.FirstName,
            MiddleName = model.MiddleName,
            LastName = model.LastName,
            SecondLastName = model.SecondLastName,
            PhoneNumber = model.PhoneNumber,
            IsActive = model.IsActive,
            IsLocked = model.IsLocked,
            NewPassword = string.IsNullOrWhiteSpace(model.NewPassword) ? null : model.NewPassword,
            RoleNames = model.RoleNames ?? new List<string>()
        };

        var (ok, err) = await _users.UpdateAsync(dto, cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, err ?? "No se pudo actualizar el usuario.");
            ViewData["Title"] = "Editar usuario";
            ViewData["PageSubtitle"] = model.Email;
            ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Usuarios</a></li><li class=\"breadcrumb-item active\">Editar</li>";
            return View(model);
        }

        TempData["Success"] = "Usuario actualizado correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(string id, bool active, CancellationToken cancellationToken)
    {
        var (ok, err) = await _users.SetActiveAsync(id, active, cancellationToken);
        if (!ok)
        {
            TempData["Error"] = err ?? "No se pudo cambiar el estado.";
        }
        else
        {
            TempData["Success"] = active ? "Usuario activado." : "Usuario desactivado.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockUser(string id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        await _userManager.ResetAccessFailedCountAsync(user);
        await _userManager.SetLockoutEndDateAsync(user, null);

        TempData["Success"] = "Usuario desbloqueado correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.UsersManage)]
    public async Task<IActionResult> SendPasswordReset(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, Request.Scheme);
        TempData["ResetLink"] = resetUrl;
        TempData["Success"] = "Enlace de restablecimiento generado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<IReadOnlyList<string>> GetRoleNameListAsync(CancellationToken cancellationToken)
    {
        var roles = await _roles.GetAllAsync(cancellationToken);
        return roles.OrderBy(r => r.Name).Select(r => r.Name).ToList();
    }
}
