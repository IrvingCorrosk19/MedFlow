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
    public async Task<IActionResult> Index(string? search, bool? isActive, CancellationToken cancellationToken)
    {
        var doctors = await _doctorService.GetAllAsync(search, isActive, cancellationToken: cancellationToken);
        ViewBag.Search = search;
        ViewBag.IsActive = isActive;
        return View(doctors);
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
