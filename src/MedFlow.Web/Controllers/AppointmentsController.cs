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
    public async Task<IActionResult> Calendar(CancellationToken cancellationToken)
    {
        var doctors = await _doctorService.GetAllAsync(null, true, cancellationToken: cancellationToken);
        ViewBag.Doctors = doctors;
        ViewData["Title"] = "Calendario de citas";
        ViewData["PageSubtitle"] = "Vista de agenda por semana/mes";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Citas</a></li><li class=\"breadcrumb-item active\">Calendario</li>";
        return View();
    }

    [RequirePermission(PermissionCodes.AppointmentsView)]
    public async Task<IActionResult> CalendarFeed(DateTime start, DateTime end, Guid? doctorId, CancellationToken cancellationToken)
    {
        var appointments = await _appointmentService.GetAllAsync(start, end, doctorId, null, cancellationToken);

        var events = appointments.Select(a =>
        {
            var color = a.Status switch
            {
                AppointmentStatus.Completed => "#28a745",
                AppointmentStatus.Cancelled => "#dc3545",
                AppointmentStatus.NoShow    => "#fd7e14",
                AppointmentStatus.Confirmed => "#007bff",
                _                           => "#6c757d"
            };
            var startDt = a.ScheduledDate.Date + a.StartTime;
            var endDt   = a.ScheduledDate.Date + a.EndTime;
            return new
            {
                id    = a.Id,
                title = $"{a.Patient?.NombreCompleto ?? "—"} · {a.Doctor?.FullName ?? "—"}",
                start = startDt.ToString("yyyy-MM-ddTHH:mm:ss"),
                end   = endDt.ToString("yyyy-MM-ddTHH:mm:ss"),
                color,
                extendedProps = new
                {
                    patientName = a.Patient?.NombreCompleto ?? "—",
                    doctorName  = a.Doctor?.FullName ?? "—",
                    reason      = a.Reason ?? "—",
                    room        = a.ConsultationRoom ?? "—",
                    status      = a.Status.ToString(),
                    detailsUrl  = Url.Action(nameof(Details), new { id = a.Id })
                }
            };
        });

        return Json(events);
    }

    [RequirePermission(PermissionCodes.AppointmentsView)]
    public async Task<IActionResult> Today(Guid? doctorId, int? status, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var appointments = await _appointmentService.GetAllAsync(today, today, doctorId, null, cancellationToken);
        var doctors = await _doctorService.GetAllAsync(null, true, cancellationToken: cancellationToken);

        if (status.HasValue)
            appointments = appointments.Where(a => (int)a.Status == status.Value).ToList();

        ViewBag.Appointments = appointments;
        ViewBag.Doctors = doctors;
        ViewBag.DoctorId = doctorId;
        ViewBag.Status = status;
        ViewData["Title"] = "Citas de hoy";
        ViewData["PageSubtitle"] = today.ToString("dddd, d 'de' MMMM yyyy",
            new System.Globalization.CultureInfo("es-ES"));
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Citas</a></li><li class=\"breadcrumb-item active\">Hoy</li>";
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
    public async Task<IActionResult> Create(DateTime? scheduledDate, CancellationToken cancellationToken)
    {
        var patients = await _patientService.GetAllAsync(null, true, cancellationToken: cancellationToken);
        var doctors = await _doctorService.GetAllAsync(null, true, cancellationToken: cancellationToken);
        ViewBag.Patients = patients;
        ViewBag.Doctors = doctors;
        var date = scheduledDate?.Date ?? DateTime.Today;
        var time = scheduledDate.HasValue ? scheduledDate.Value.TimeOfDay : TimeSpan.FromHours(8);
        return View(new AppointmentViewModel
        {
            ScheduledDate = date,
            StartTime = time,
            EndTime = time.Add(TimeSpan.FromMinutes(30))
        });
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
        {
            TempData["Success"] = "Cita marcada como completada.";
            // Suggest creating invoice if patient doesn't already have an open one for this appointment
            TempData["SuggestBilling"] = appointment.PatientId.ToString();
            TempData["SuggestBillingAptId"] = appointment.Id.ToString();
        }
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

    [RequirePermission(PermissionCodes.AppointmentsView)]
    public async Task<IActionResult> ExportCsv(DateTime? from, DateTime? to, Guid? doctorId, Guid? patientId, CancellationToken cancellationToken = default)
    {
        var fromDate = from ?? DateTime.Today.AddDays(-30);
        var toDate   = to   ?? DateTime.Today;
        var appointments = await _appointmentService.GetAllAsync(fromDate, toDate, doctorId, patientId, cancellationToken);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Fecha,Hora inicio,Hora fin,Paciente,Doctor,Estado,Motivo,Consultorio");
        foreach (var a in appointments)
        {
            sb.AppendLine(string.Join(",",
                CsvQ(a.ScheduledDate.ToString("dd/MM/yyyy")),
                CsvQ(a.StartTime.ToString(@"hh\:mm")),
                CsvQ(a.EndTime.ToString(@"hh\:mm")),
                CsvQ(a.Patient?.NombreCompleto ?? ""),
                CsvQ(a.Doctor?.FullName ?? ""),
                CsvQ(a.Status.ToString()),
                CsvQ(a.Reason ?? ""),
                CsvQ(a.ConsultationRoom ?? "")));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"citas_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.csv");
    }

    private static string CsvQ(string v) =>
        (v.Contains(',') || v.Contains('"') || v.Contains('\n'))
            ? "\"" + v.Replace("\"", "\"\"") + "\""
            : v;

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AppointmentsEdit)]
    public async Task<IActionResult> Reschedule(Guid id, DateTime newDate, TimeSpan newStart, TimeSpan newEnd, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentService.GetByIdAsync(id, cancellationToken);
        if (appointment == null) return NotFound();

        if (newEnd <= newStart)
        {
            TempData["Error"] = "La hora de fin debe ser posterior a la hora de inicio.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (newDate.Date < DateTime.UtcNow.Date)
        {
            TempData["Error"] = "No se puede reagendar a una fecha pasada.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var hasConflict = await _appointmentService.HasConflictAsync(
            appointment.DoctorId, newDate, newStart, newEnd, id, cancellationToken);
        if (hasConflict)
        {
            TempData["Error"] = "El doctor tiene otro turno en ese horario. Elija otra hora.";
            return RedirectToAction(nameof(Details), new { id });
        }

        appointment.ScheduledDate = newDate;
        appointment.StartTime = newStart;
        appointment.EndTime = newEnd;
        var (success, error) = await _appointmentService.UpdateAsync(appointment, cancellationToken);
        if (success)
            TempData["Success"] = $"Cita reagendada al {newDate:dd/MM/yyyy} {newStart:hh\\:mm}.";
        else
            TempData["Error"] = error ?? "No se pudo reagendar la cita.";

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
