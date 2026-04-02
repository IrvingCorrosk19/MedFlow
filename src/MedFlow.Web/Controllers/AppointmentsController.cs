using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Web.Authorization;
using MedFlow.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
public class AppointmentsController : Controller
{
    private readonly IAppointmentService _appointmentService;
    private readonly IPatientService _patientService;
    private readonly IDoctorService _doctorService;

    public AppointmentsController(IAppointmentService appointmentService, IPatientService patientService, IDoctorService doctorService)
    {
        _appointmentService = appointmentService;
        _patientService = patientService;
        _doctorService = doctorService;
    }

    [RequirePermission(PermissionCodes.AppointmentsView)]
    public async Task<IActionResult> Index(DateTime? from, DateTime? to, Guid? doctorId, Guid? patientId, int? status, CancellationToken cancellationToken)
    {
        var fromDate = from ?? DateTime.Today;
        var toDate = to ?? DateTime.Today.AddDays(7);
        var appointments = await _appointmentService.GetAllAsync(fromDate, toDate, doctorId, patientId, cancellationToken);
        var doctors = await _doctorService.GetAllAsync(null, true, cancellationToken: cancellationToken);

        if (status.HasValue)
            appointments = appointments.Where(a => (int)a.Status == status.Value).ToList();

        ViewBag.Appointments = appointments;
        ViewBag.Doctors = doctors;
        ViewBag.From = fromDate;
        ViewBag.To = toDate;
        ViewBag.DoctorId = doctorId;
        ViewBag.PatientId = patientId;
        ViewBag.Status = status;

        return View();
    }

    [RequirePermission(PermissionCodes.AppointmentsView)]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentService.GetByIdAsync(id, cancellationToken);
        if (appointment == null) return NotFound();
        ViewData["Title"] = $"Cita · {appointment.ScheduledDate:dd/MM/yyyy}";
        ViewData["PageSubtitle"] = $"{appointment.Patient?.NombreCompleto ?? "—"} — {appointment.Doctor?.FullName ?? "—"}";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Citas</a></li><li class=\"breadcrumb-item active\">Detalle</li>";
        return View(appointment);
    }

    [RequirePermission(PermissionCodes.AppointmentsCreate)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var patients = await _patientService.GetAllAsync(null, true, cancellationToken: cancellationToken);
        var doctors = await _doctorService.GetAllAsync(null, true, cancellationToken: cancellationToken);
        ViewBag.Patients = patients;
        ViewBag.Doctors = doctors;
        return View(new AppointmentViewModel { ScheduledDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AppointmentsCreate)]
    public async Task<IActionResult> Create(AppointmentViewModel model, CancellationToken cancellationToken)
    {
        if (model.EndTime <= model.StartTime)
            ModelState.AddModelError(nameof(model.EndTime), "La hora de fin debe ser posterior a la hora de inicio.");

        if (model.ScheduledDate.Date < DateTime.UtcNow.Date)
            ModelState.AddModelError(nameof(model.ScheduledDate), "No se puede crear una cita en una fecha pasada.");

        var patient = await _patientService.GetByIdAsync(model.PatientId, cancellationToken);
        if (patient == null || !patient.IsActive)
            ModelState.AddModelError(nameof(model.PatientId), "El paciente seleccionado no está activo.");

        var doctor = await _doctorService.GetByIdAsync(model.DoctorId, cancellationToken);
        if (doctor == null || !doctor.IsActive)
            ModelState.AddModelError(nameof(model.DoctorId), "El doctor seleccionado no está activo.");

        if (ModelState.IsValid)
        {
            var appointment = MapToEntity(model);
            var (success, error) = await _appointmentService.CreateAsync(appointment, cancellationToken);
            if (success)
            {
                TempData["Success"] = "Cita agendada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", error ?? "Error al agendar la cita.");
        }

        var patients = await _patientService.GetAllAsync(null, true, cancellationToken: cancellationToken);
        var doctors = await _doctorService.GetAllAsync(null, true, cancellationToken: cancellationToken);
        ViewBag.Patients = patients;
        ViewBag.Doctors = doctors;
        return View(model);
    }

    [RequirePermission(PermissionCodes.AppointmentsEdit)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentService.GetByIdAsync(id, cancellationToken);
        if (appointment == null) return NotFound();

        var patients = await _patientService.GetAllAsync(null, true, cancellationToken: cancellationToken);
        var doctors = await _doctorService.GetAllAsync(null, true, cancellationToken: cancellationToken);
        ViewBag.Patients = patients;
        ViewBag.Doctors = doctors;

        return View(MapToViewModel(appointment));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AppointmentsEdit)]
    public async Task<IActionResult> Edit(Guid id, AppointmentViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return NotFound();

        if (model.EndTime <= model.StartTime)
            ModelState.AddModelError(nameof(model.EndTime), "La hora de fin debe ser posterior a la hora de inicio.");

        if (ModelState.IsValid)
        {
            var appointment = await _appointmentService.GetByIdAsync(id, cancellationToken);
            if (appointment == null) return NotFound();

            MapToEntity(model, appointment);
            var (success, error) = await _appointmentService.UpdateAsync(appointment, cancellationToken);
            if (success)
            {
                TempData["Success"] = "Cita actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", error ?? "Error al actualizar la cita.");
        }

        var patients = await _patientService.GetAllAsync(null, true, cancellationToken: cancellationToken);
        var doctors = await _doctorService.GetAllAsync(null, true, cancellationToken: cancellationToken);
        ViewBag.Patients = patients;
        ViewBag.Doctors = doctors;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AppointmentsEdit)]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentService.GetByIdAsync(id, cancellationToken);
        if (appointment == null) return NotFound();
        appointment.Status = AppointmentStatus.Confirmed;
        var (success, error) = await _appointmentService.UpdateAsync(appointment, cancellationToken);
        if (success)
            TempData["Success"] = "Cita confirmada.";
        else
            TempData["Error"] = error ?? "No se pudo confirmar la cita.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AppointmentsEdit)]
    public async Task<IActionResult> MarkCompleted(Guid id, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentService.GetByIdAsync(id, cancellationToken);
        if (appointment == null) return NotFound();
        appointment.Status = AppointmentStatus.Completed;
        var (success, error) = await _appointmentService.UpdateAsync(appointment, cancellationToken);
        if (success)
            TempData["Success"] = "Cita marcada como completada.";
        else
            TempData["Error"] = error ?? "No se pudo completar la cita.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AppointmentsEdit)]
    public async Task<IActionResult> MarkNoShow(Guid id, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentService.GetByIdAsync(id, cancellationToken);
        if (appointment == null) return NotFound();
        appointment.Status = AppointmentStatus.NoShow;
        var (success, error) = await _appointmentService.UpdateAsync(appointment, cancellationToken);
        if (success)
            TempData["Success"] = "Cita registrada como no-show.";
        else
            TempData["Error"] = error ?? "No se pudo registrar el no-show.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AppointmentsCancel)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _appointmentService.DeleteAsync(id, cancellationToken);
            TempData["Success"] = "Cita eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            TempData["Error"] = "No se pudo eliminar la cita.";
            return RedirectToAction(nameof(Index));
        }
    }

    private static Appointment MapToEntity(AppointmentViewModel vm, Appointment? entity = null)
    {
        entity ??= new Appointment();
        entity.PatientId = vm.PatientId;
        entity.DoctorId = vm.DoctorId;
        entity.ScheduledDate = vm.ScheduledDate;
        entity.StartTime = vm.StartTime;
        entity.EndTime = vm.EndTime;
        entity.Reason = vm.Reason;
        entity.Notes = vm.Notes;
        entity.ConsultationRoom = vm.ConsultationRoom;
        entity.Status = vm.Status;
        return entity;
    }

    private static AppointmentViewModel MapToViewModel(Appointment entity) => new()
    {
        Id = entity.Id,
        PatientId = entity.PatientId,
        DoctorId = entity.DoctorId,
        PatientName = entity.Patient?.FullName,
        DoctorName = entity.Doctor?.FullName,
        ScheduledDate = entity.ScheduledDate,
        StartTime = entity.StartTime,
        EndTime = entity.EndTime,
        Reason = entity.Reason,
        Notes = entity.Notes,
        ConsultationRoom = entity.ConsultationRoom,
        Status = entity.Status
    };
}
