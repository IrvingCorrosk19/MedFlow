using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
[Route("SuperAdmin")]
public class LegacyRoutesController : Controller
{
    [HttpGet("Patients")]
    public IActionResult Patients() => RedirectToAction("Index", "Patients", new { area = "" });

    [HttpGet("Doctors")]
    public IActionResult Doctors() => RedirectToAction("Index", "Doctors", new { area = "" });

    [HttpGet("Appointments")]
    public IActionResult Appointments() => RedirectToAction("Index", "Appointments", new { area = "" });

    [HttpGet("MedicalRecords")]
    public IActionResult MedicalRecords() => RedirectToAction("Index", "MedicalRecords", new { area = "" });

    [HttpGet("Reports/Appointments")]
    public IActionResult ReportsAppointments() => RedirectToAction("Appointments", "Reports", new { area = "" });
}
