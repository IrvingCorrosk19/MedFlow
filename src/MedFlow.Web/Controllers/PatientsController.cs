using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Domain.Entities;
using MedFlow.Web.Authorization;
using MedFlow.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

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
    public async Task<IActionResult> Index(
        string? search, bool? estadoActivo,
        string? documento, string? telefono,
        int? edadDesde, int? edadHasta,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Pacientes";
        ViewData["PageSubtitle"] = "Directorio de pacientes de la clínica";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Pacientes</li>";

        var patients = await _patientService.GetAllAsync(
            search, estadoActivo,
            documento: documento, telefono: telefono,
            edadDesde: edadDesde, edadHasta: edadHasta,
            cancellationToken: cancellationToken);

        ViewBag.Search = search;
        ViewBag.EstadoActivo = estadoActivo;
        ViewBag.Documento = documento;
        ViewBag.Telefono = telefono;
        ViewBag.EdadDesde = edadDesde;
        ViewBag.EdadHasta = edadHasta;
        return View(patients);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.PatientsView)]
    public async Task<IActionResult> ExportCsv(
        string? search, bool? estadoActivo,
        string? documento, string? telefono,
        int? edadDesde, int? edadHasta,
        CancellationToken cancellationToken = default)
    {
        var list = await _patientService.GetAllAsync(
            search, estadoActivo,
            documento: documento, telefono: telefono,
            edadDesde: edadDesde, edadHasta: edadHasta,
            cancellationToken: cancellationToken);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Nombre,TipoDocumento,NumeroDocumento,Teléfono,Correo,FechaNacimiento,Sexo,Activo");
        foreach (var p in list)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(p.NombreCompleto ?? ""),
                CsvEscape(p.TipoDocumento ?? ""),
                CsvEscape(p.NumeroDocumento ?? ""),
                CsvEscape(p.Telefono ?? ""),
                CsvEscape(p.Correo ?? ""),
                CsvEscape(p.FechaNacimiento?.ToString("dd/MM/yyyy") ?? ""),
                CsvEscape(p.Sexo ?? ""),
                p.IsActive ? "Sí" : "No"));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();

        return File(bytes, "text/csv", $"pacientes_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.PatientsCreate)]
    public IActionResult ImportCsv()
    {
        ViewData["Title"] = "Importar pacientes";
        ViewData["PageSubtitle"] = "Carga masiva desde archivo CSV";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Pacientes</a></li><li class=\"breadcrumb-item active\">Importar CSV</li>";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.PatientsCreate)]
    public async Task<IActionResult> ImportCsv(IFormFile file, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Importar pacientes";
        ViewData["PageSubtitle"] = "Carga masiva desde archivo CSV";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Pacientes</a></li><li class=\"breadcrumb-item active\">Importar CSV</li>";

        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Seleccione un archivo CSV.");
            return View();
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "El archivo debe ser de tipo .csv");
            return View();
        }

        var results = new List<(int Row, string? Error, string? Name)>();
        int created = 0, skipped = 0;

        using var reader = new System.IO.StreamReader(file.OpenReadStream(), System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        string? headerLine = await reader.ReadLineAsync(cancellationToken);
        if (headerLine == null)
        {
            ModelState.AddModelError(string.Empty, "El archivo está vacío.");
            return View();
        }

        int row = 1;
        while (!reader.EndOfStream)
        {
            row++;
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = ParseCsvLine(line);
            // Expected: PrimerNombre,SegundoNombre,PrimerApellido,SegundoApellido,FechaNacimiento,Sexo,TipoDocumento,NumeroDocumento,Telefono,Correo,Direccion,Alergias,Observaciones
            if (cols.Length < 3)
            {
                results.Add((row, "Faltan columnas mínimas (PrimerNombre, PrimerApellido)", null));
                skipped++;
                continue;
            }

            var primerNombre = cols.ElementAtOrDefault(0)?.Trim() ?? "";
            var segundoNombre = cols.ElementAtOrDefault(1)?.Trim();
            var primerApellido = cols.ElementAtOrDefault(2)?.Trim() ?? "";
            var segundoApellido = cols.ElementAtOrDefault(3)?.Trim();
            var fechaStr = cols.ElementAtOrDefault(4)?.Trim();
            var sexo = cols.ElementAtOrDefault(5)?.Trim();
            var tipoDoc = cols.ElementAtOrDefault(6)?.Trim();
            var numDoc = cols.ElementAtOrDefault(7)?.Trim();
            var telefono = cols.ElementAtOrDefault(8)?.Trim();
            var correo = cols.ElementAtOrDefault(9)?.Trim();
            var direccion = cols.ElementAtOrDefault(10)?.Trim();
            var alergias = cols.ElementAtOrDefault(11)?.Trim();
            var observaciones = cols.ElementAtOrDefault(12)?.Trim();

            if (string.IsNullOrWhiteSpace(primerNombre) || string.IsNullOrWhiteSpace(primerApellido))
            {
                results.Add((row, "PrimerNombre y PrimerApellido son obligatorios", null));
                skipped++;
                continue;
            }

            DateTime? fechaNac = null;
            if (!string.IsNullOrWhiteSpace(fechaStr))
            {
                if (DateTime.TryParseExact(fechaStr, new[] { "dd/MM/yyyy", "yyyy-MM-dd", "MM/dd/yyyy" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var fd))
                    fechaNac = fd;
                else
                {
                    results.Add((row, $"Fecha inválida: '{fechaStr}' (use dd/MM/yyyy)", $"{primerNombre} {primerApellido}"));
                    skipped++;
                    continue;
                }
            }

            var patient = new Patient
            {
                PrimerNombre = primerNombre,
                SegundoNombre = string.IsNullOrWhiteSpace(segundoNombre) ? null : segundoNombre,
                PrimerApellido = primerApellido,
                SegundoApellido = string.IsNullOrWhiteSpace(segundoApellido) ? null : segundoApellido,
                FechaNacimiento = fechaNac,
                Sexo = string.IsNullOrWhiteSpace(sexo) ? null : sexo,
                TipoDocumento = string.IsNullOrWhiteSpace(tipoDoc) ? null : tipoDoc,
                NumeroDocumento = string.IsNullOrWhiteSpace(numDoc) ? null : numDoc,
                Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono,
                Correo = string.IsNullOrWhiteSpace(correo) ? null : correo,
                Direccion = string.IsNullOrWhiteSpace(direccion) ? null : direccion,
                Alergias = string.IsNullOrWhiteSpace(alergias) ? null : alergias,
                Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones,
                IsActive = true
            };

            var (p, err) = await _patientService.CreateAsync(patient, cancellationToken);
            if (p != null)
            {
                created++;
                results.Add((row, null, p.NombreCompleto));
            }
            else
            {
                skipped++;
                results.Add((row, err ?? "Error desconocido", $"{primerNombre} {primerApellido}"));
            }
        }

        ViewBag.ImportResults = results;
        ViewBag.ImportCreated = created;
        ViewBag.ImportSkipped = skipped;

        if (created > 0)
            TempData["Success"] = $"{created} paciente(s) importado(s) correctamente.";

        return View();
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.PatientsCreate)]
    public IActionResult ImportCsvTemplate()
    {
        var csv = "PrimerNombre,SegundoNombre,PrimerApellido,SegundoApellido,FechaNacimiento,Sexo,TipoDocumento,NumeroDocumento,Telefono,Correo,Direccion,Alergias,Observaciones\r\n" +
                  "Juan,Carlos,Pérez,García,15/03/1985,M,CC,12345678,3001234567,juan.perez@email.com,Calle 123,Penicilina,\r\n" +
                  "María,,López,,22/07/1990,F,CC,98765432,3009876543,maria.lopez@email.com,,,\r\n";
        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv", "plantilla_pacientes.csv");
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                { current.Append('"'); i++; }
                else
                    inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            { fields.Add(current.ToString()); current.Clear(); }
            else
                current.Append(c);
        }
        fields.Add(current.ToString());
        return fields.ToArray();
    }

    [RequirePermission(PermissionCodes.PatientsView)]
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
        if (model.FechaNacimiento.HasValue)
        {
            if (model.FechaNacimiento.Value > DateTime.Today)
                ModelState.AddModelError(nameof(model.FechaNacimiento), "La fecha de nacimiento no puede ser una fecha futura.");
            else if (model.FechaNacimiento.Value < DateTime.Today.AddYears(-150))
                ModelState.AddModelError(nameof(model.FechaNacimiento), "La fecha de nacimiento no es válida.");
        }

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

    [RequirePermission(PermissionCodes.PatientsEdit)]
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

        if (model.FechaNacimiento.HasValue)
        {
            if (model.FechaNacimiento.Value > DateTime.Today)
                ModelState.AddModelError(nameof(model.FechaNacimiento), "La fecha de nacimiento no puede ser una fecha futura.");
            else if (model.FechaNacimiento.Value < DateTime.Today.AddYears(-150))
                ModelState.AddModelError(nameof(model.FechaNacimiento), "La fecha de nacimiento no es válida.");
        }

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
        try
        {
            await _patientService.DeleteAsync(id, cancellationToken);
            TempData["Success"] = "Paciente dado de baja correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message.Contains("FOREIGN KEY") || ex.Message.Contains("foreign key")
                ? "No se puede eliminar el paciente porque tiene registros asociados (citas, expedientes o facturas)."
                : "No se pudo eliminar el paciente: " + ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
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
