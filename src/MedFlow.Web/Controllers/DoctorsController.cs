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

    public DoctorsController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
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
        return View(doctor);
    }

    [RequirePermission(PermissionCodes.DoctorsCreate)]
    public IActionResult Create() => View(new DoctorViewModel());

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
                    return View(model);
                }
            }

            var doctor = MapToEntity(model);
            var (created, err) = await _doctorService.CreateAsync(doctor, cancellationToken);
            if (created == null)
            {
                ModelState.AddModelError(string.Empty, err ?? "No se pudo registrar el doctor.");
                return View(model);
            }

            TempData["Success"] = "Doctor registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    [RequirePermission(PermissionCodes.DoctorsEdit)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var doctor = await _doctorService.GetByIdAsync(id, cancellationToken);
        if (doctor == null) return NotFound();
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
}
