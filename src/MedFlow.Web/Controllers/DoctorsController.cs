using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Domain.Entities;
using MedFlow.Web.Authorization;
using MedFlow.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
public class DoctorsController : Controller
{
    private readonly IDoctorService _doctorService;
    private readonly ITenantContext _tenant;

    public DoctorsController(IDoctorService doctorService, ITenantContext tenant)
    {
        _doctorService = doctorService;
        _tenant = tenant;
    }

    private string PageSubtitleWithTenant(string line)
    {
        if (_tenant.TenantName != null && !string.IsNullOrWhiteSpace(_tenant.TenantCode))
            return $"{line} · {_tenant.TenantName} ({_tenant.TenantCode})";
        return line;
    }

    [RequirePermission(PermissionCodes.DoctorsView)]
    public async Task<IActionResult> Index(string? search, bool? isActive, string? specialty, CancellationToken cancellationToken)
    {
        var doctors = await _doctorService.GetAllAsync(search, isActive, cancellationToken: cancellationToken);

        var specialtyList = doctors
            .Where(d => !string.IsNullOrWhiteSpace(d.Speciality))
            .Select(d => d.Speciality!)
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        if (!string.IsNullOrWhiteSpace(specialty))
            doctors = doctors.Where(d => d.Speciality == specialty).ToList();

        ViewData["Title"] = "Doctores";
        ViewData["PageSubtitle"] = PageSubtitleWithTenant("Directorio médico");
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Doctores</li>";

        ViewBag.Search = search;
        ViewBag.IsActive = isActive;
        ViewBag.SpecialtyList = specialtyList;
        ViewBag.SelectedSpecialty = specialty;
        return View(doctors);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.DoctorsView)]
    public async Task<IActionResult> ExportCsv(string? search, bool? active, string? specialty, CancellationToken cancellationToken = default)
    {
        var doctors = await _doctorService.GetAllAsync(search, active, cancellationToken: cancellationToken);

        if (!string.IsNullOrWhiteSpace(specialty))
            doctors = doctors.Where(d => d.Speciality == specialty).ToList();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Nombre,Especialidad,Teléfono,Correo,Activo");
        foreach (var d in doctors)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(d.FullName),
                CsvEscape(d.Speciality ?? ""),
                CsvEscape(d.Phone ?? ""),
                CsvEscape(d.Email ?? ""),
                d.IsActive ? "Sí" : "No"));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();

        return File(bytes, "text/csv", $"doctores_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    [RequirePermission(PermissionCodes.DoctorsView)]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var doctor = await _doctorService.GetByIdAsync(id, cancellationToken);
        if (doctor == null) return NotFound();

        ViewData["Title"] = "Detalle de doctor";
        ViewData["PageSubtitle"] = PageSubtitleWithTenant(doctor.FullName ?? "");
        ViewData["Breadcrumb"] =
            "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Doctores</a></li><li class=\"breadcrumb-item active\">Detalle</li>";

        return View(doctor);
    }

    [RequirePermission(PermissionCodes.DoctorsCreate)]
    public IActionResult Create()
    {
        ViewData["Title"] = "Nuevo doctor";
        ViewData["PageSubtitle"] = PageSubtitleWithTenant("Registro de personal médico");
        ViewData["Breadcrumb"] =
            "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Doctores</a></li><li class=\"breadcrumb-item active\">Nuevo</li>";
        return View(new DoctorViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.DoctorsCreate)]
    public async Task<IActionResult> Create(DoctorViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            if (!string.IsNullOrWhiteSpace(model.LicenseNumber))
            {
                var existing = await _doctorService.GetAllAsync(cancellationToken: cancellationToken);
                if (existing.Any(d => d.LicenseNumber != null &&
                    d.LicenseNumber.Equals(model.LicenseNumber, StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError(nameof(model.LicenseNumber),
                        "Ya existe un doctor con ese número de licencia médica.");
                    SetCreateViewData();
                    return View(model);
                }
            }

            var doctor = MapToEntity(model);
            var (created, err) = await _doctorService.CreateAsync(doctor, cancellationToken);
            if (created == null)
            {
                ModelState.AddModelError(string.Empty, err ?? "No se pudo registrar el doctor.");
                SetCreateViewData();
                return View(model);
            }

            TempData["Success"] = "Doctor registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        SetCreateViewData();
        return View(model);
    }

    [RequirePermission(PermissionCodes.DoctorsEdit)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var doctor = await _doctorService.GetByIdAsync(id, cancellationToken);
        if (doctor == null) return NotFound();
        SetEditViewData();
        return View(MapToViewModel(doctor));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.DoctorsEdit)]
    public async Task<IActionResult> Edit(Guid id, DoctorViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return NotFound();
        if (ModelState.IsValid)
        {
            if (!string.IsNullOrWhiteSpace(model.LicenseNumber))
            {
                var existing = await _doctorService.GetAllAsync(cancellationToken: cancellationToken);
                if (existing.Any(d => d.Id != model.Id && d.LicenseNumber != null &&
                    d.LicenseNumber.Equals(model.LicenseNumber, StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError(nameof(model.LicenseNumber),
                        "Ya existe un doctor con ese número de licencia médica.");
                    SetEditViewData();
                    return View(model);
                }
            }

            var doctor = await _doctorService.GetByIdAsync(id, cancellationToken);
            if (doctor == null) return NotFound();

            MapToEntity(model, doctor);
            await _doctorService.UpdateAsync(doctor, cancellationToken);
            TempData["Success"] = "Doctor actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        SetEditViewData();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.DoctorsEdit)]
    public async Task<IActionResult> SetActive(Guid id, bool active, CancellationToken cancellationToken = default)
    {
        var doctor = await _doctorService.GetByIdAsync(id, cancellationToken);
        if (doctor == null) return NotFound();
        doctor.IsActive = active;
        await _doctorService.UpdateAsync(doctor, cancellationToken);
        TempData["Success"] = active ? "Doctor activado." : "Doctor desactivado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.DoctorsDelete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _doctorService.DeleteAsync(id, cancellationToken);
        TempData["Success"] = "Doctor eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private static Doctor MapToEntity(DoctorViewModel vm, Doctor? entity = null)
    {
        entity ??= new Doctor();
        entity.FirstName = vm.FirstName;
        entity.LastName = vm.LastName;
        entity.Speciality = vm.Speciality;
        entity.LicenseNumber = vm.LicenseNumber;
        entity.Phone = vm.Phone;
        entity.Email = vm.Email;
        entity.WorkingHours = vm.WorkingHours;
        entity.ConsultationRoom = vm.ConsultationRoom;
        entity.Notes = vm.Notes;
        entity.IsActive = vm.IsActive;
        return entity;
    }

    private static DoctorViewModel MapToViewModel(Doctor entity) => new()
    {
        Id = entity.Id,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        Speciality = entity.Speciality,
        LicenseNumber = entity.LicenseNumber,
        Phone = entity.Phone,
        Email = entity.Email,
        WorkingHours = entity.WorkingHours,
        ConsultationRoom = entity.ConsultationRoom,
        Notes = entity.Notes,
        IsActive = entity.IsActive
    };

    private void SetCreateViewData()
    {
        ViewData["Title"] = "Nuevo doctor";
        ViewData["PageSubtitle"] = PageSubtitleWithTenant("Registro de personal médico");
        ViewData["Breadcrumb"] =
            "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Doctores</a></li><li class=\"breadcrumb-item active\">Nuevo</li>";
    }

    private void SetEditViewData()
    {
        ViewData["Title"] = "Editar doctor";
        ViewData["PageSubtitle"] = PageSubtitleWithTenant("Actualización de información profesional");
        ViewData["Breadcrumb"] =
            "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Doctores</a></li><li class=\"breadcrumb-item active\">Editar</li>";
    }
}
