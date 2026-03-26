using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Domain.Entities;
using MedFlow.Web.Authorization;
using MedFlow.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
public class PatientsController : Controller
{
    private readonly IPatientService _patientService;
    private readonly IPatientPortalEnableService _portalEnable;

    public PatientsController(IPatientService patientService, IPatientPortalEnableService portalEnable)
    {
        _patientService = patientService;
        _portalEnable = portalEnable;
    }

    [RequirePermission(PermissionCodes.PatientsView)]
    public async Task<IActionResult> Index(string? search, bool? estadoActivo, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Pacientes";
        ViewData["PageSubtitle"] = "Directorio de pacientes de la clínica";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Pacientes</li>";

        var patients = await _patientService.GetAllAsync(search, estadoActivo, cancellationToken: cancellationToken);
        ViewBag.Search = search;
        ViewBag.EstadoActivo = estadoActivo;
        return View(patients);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _patientService.GetByIdAsync(id, cancellationToken);
        if (patient == null) return NotFound();

        ViewData["Title"] = patient.NombreCompleto;
        ViewData["PageSubtitle"] = "Expediente del paciente";
        ViewData["Breadcrumb"] = $"<li class=\"breadcrumb-item\"><a href=\"{Url.Action(nameof(Index))}\">Pacientes</a></li><li class=\"breadcrumb-item active\">Detalle</li>";

        return View(patient);
    }

    [RequirePermission(PermissionCodes.PatientsCreate)]
    public IActionResult Create()
    {
        ViewData["Title"] = "Nuevo paciente";
        ViewData["PageSubtitle"] = "Registro en recepción";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Pacientes</a></li><li class=\"breadcrumb-item active\">Nuevo</li>";

        return View(new PatientViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.PatientsCreate)]
    public async Task<IActionResult> Create(PatientViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var patient = MapToEntity(model);
            var (created, err) = await _patientService.CreateAsync(patient, cancellationToken);
            if (created == null)
            {
                ModelState.AddModelError(string.Empty, err ?? "No se pudo registrar el paciente.");
                ViewData["Title"] = "Nuevo paciente";
                ViewData["PageSubtitle"] = "Registro en recepción";
                ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Pacientes</a></li><li class=\"breadcrumb-item active\">Nuevo</li>";
                return View(model);
            }

            TempData["Success"] = "Paciente registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = "Nuevo paciente";
        ViewData["PageSubtitle"] = "Registro en recepción";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Pacientes</a></li><li class=\"breadcrumb-item active\">Nuevo</li>";
        return View(model);
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _patientService.GetByIdAsync(id, cancellationToken);
        if (patient == null) return NotFound();

        ViewData["Title"] = "Editar paciente";
        ViewData["PageSubtitle"] = patient.NombreCompleto;
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Pacientes</a></li><li class=\"breadcrumb-item active\">Editar</li>";

        return View(MapToViewModel(patient));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.PatientsEdit)]
    public async Task<IActionResult> Edit(Guid id, PatientViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return NotFound();
        if (ModelState.IsValid)
        {
            var patient = await _patientService.GetByIdAsync(id, cancellationToken);
            if (patient == null) return NotFound();

            MapToEntity(model, patient);
            await _patientService.UpdateAsync(patient, cancellationToken);
            TempData["Success"] = "Paciente actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = "Editar paciente";
        ViewData["PageSubtitle"] = "Corrija los datos";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Pacientes</a></li><li class=\"breadcrumb-item active\">Editar</li>";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.PatientsDelete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _patientService.DeleteAsync(id, cancellationToken);
        TempData["Success"] = "Paciente dado de baja correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.PatientsEdit)]
    public async Task<IActionResult> EnablePortal(Guid id, string? password, CancellationToken cancellationToken = default)
    {
        var (success, _, tempPwd, error) = await _portalEnable.EnablePortalForPatientAsync(id, password, cancellationToken);
        if (success)
        {
            TempData["Success"] = string.IsNullOrEmpty(tempPwd)
                ? "Portal habilitado. El paciente puede iniciar sesión con su correo."
                : $"Portal habilitado. Contraseña temporal para el paciente: {tempPwd} (guárdela, no se mostrará de nuevo).";
        }
        else
        {
            TempData["Error"] = error ?? "No se pudo habilitar el portal.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.PatientsEdit)]
    public async Task<IActionResult> DisablePortal(Guid id, CancellationToken cancellationToken = default)
    {
        if (await _portalEnable.DisablePortalForPatientAsync(id, cancellationToken))
            TempData["Success"] = "Acceso al portal deshabilitado.";
        else
            TempData["Error"] = "No se pudo deshabilitar el portal.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private static Patient MapToEntity(PatientViewModel vm, Patient? entity = null)
    {
        entity ??= new Patient();
        entity.PrimerNombre = vm.PrimerNombre.Trim();
        entity.SegundoNombre = string.IsNullOrWhiteSpace(vm.SegundoNombre) ? null : vm.SegundoNombre.Trim();
        entity.PrimerApellido = vm.PrimerApellido.Trim();
        entity.SegundoApellido = string.IsNullOrWhiteSpace(vm.SegundoApellido) ? null : vm.SegundoApellido.Trim();
        entity.FechaNacimiento = vm.FechaNacimiento;
        entity.Sexo = vm.Sexo;
        entity.TipoDocumento = vm.TipoDocumento;
        entity.NumeroDocumento = vm.NumeroDocumento;
        entity.Telefono = vm.Telefono;
        entity.Correo = vm.Correo;
        entity.Direccion = vm.Direccion;
        entity.ContactoEmergenciaNombre = vm.ContactoEmergenciaNombre;
        entity.ContactoEmergenciaTelefono = vm.ContactoEmergenciaTelefono;
        entity.Alergias = vm.Alergias;
        entity.Observaciones = vm.Observaciones;
        entity.IsActive = vm.EstadoActivo;
        return entity;
    }

    private static PatientViewModel MapToViewModel(Patient entity) => new()
    {
        Id = entity.Id,
        PrimerNombre = entity.PrimerNombre,
        SegundoNombre = entity.SegundoNombre,
        PrimerApellido = entity.PrimerApellido,
        SegundoApellido = entity.SegundoApellido,
        FechaNacimiento = entity.FechaNacimiento,
        Sexo = entity.Sexo,
        TipoDocumento = entity.TipoDocumento,
        NumeroDocumento = entity.NumeroDocumento,
        Telefono = entity.Telefono,
        Correo = entity.Correo,
        Direccion = entity.Direccion,
        ContactoEmergenciaNombre = entity.ContactoEmergenciaNombre,
        ContactoEmergenciaTelefono = entity.ContactoEmergenciaTelefono,
        Alergias = entity.Alergias,
        Observaciones = entity.Observaciones,
        EstadoActivo = entity.IsActive
    };
}
