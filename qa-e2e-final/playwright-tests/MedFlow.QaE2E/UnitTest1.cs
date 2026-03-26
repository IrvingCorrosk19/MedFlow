using Microsoft.Playwright;
using Xunit;

namespace MedFlow.QaE2E;

public sealed class RoleE2ETests : IClassFixture<RoleE2ETests.PlaywrightFixture>
{
    public sealed class PlaywrightFixture : IAsyncLifetime
    {
        public IPlaywright Playwright { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        }

        public Task DisposeAsync()
        {
            Playwright?.Dispose();
            return Task.CompletedTask;
        }
    }

    private readonly PlaywrightFixture _fx;
    private const string BaseUrl = "http://localhost:5115";
    private const string QaPassword = "Medflow2026!Aa"; // Development:QaRoleUsersPassword

    public RoleE2ETests(PlaywrightFixture fx) => _fx = fx;

    private async Task<IPage> NewPageAsync(string? headlessOverride = null)
    {
        var browser = await _fx.Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headlessOverride is null ? true : headlessOverride.Equals("false", StringComparison.OrdinalIgnoreCase)
        });
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl
        });
        var page = await context.NewPageAsync();

        // Cierra diálogos nativos (confirm) automáticamente cuando aplique.
        page.Dialog += async (_, dialog) =>
        {
            // En general aceptamos para flows de cancel/delete.
            await dialog.AcceptAsync().ConfigureAwait(false);
        };

        return page;
    }

    private static string BuildShort(string prefix)
        => $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-" + Guid.NewGuid().ToString("N")[..6];

    private static async Task<string> GetFirstNonEmptySelectOptionValueAsync(IPage page, string selectCss)
    {
        var values = await page.EvaluateAsync<string[]>(
            @"(sel) => {
                const el = document.querySelector(sel);
                const opts = el ? el.options : [];
                return Array.from(opts)
                    .map(o => o.value)
                    .filter(v => v && v.trim().length > 0);
            }",
            selectCss);

        Assert.True(values.Length > 0, $"No hay opciones no vacías en el select: {selectCss}");
        return values[0];
    }

    private static async Task LoginStaffAsync(IPage page, string email)
    {
        await page.GotoAsync("/Account/Login");
        await page.Locator("input[name='Email']").FillAsync(email);
        await page.Locator("input[name='Password']").FillAsync(QaPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync(new() { Force = true });
        await page.WaitForURLAsync("**/", new PageWaitForURLOptions { Timeout = 30000 });
    }

    private static async Task LoginPatientAsync(IPage page, string email)
    {
        await page.GotoAsync("/PatientPortal/login");
        await page.Locator("input[name='Email']").FillAsync(email);
        await page.Locator("input[name='Password']").FillAsync(QaPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Iniciar sesión" }).ClickAsync(new() { Force = true });
        await page.WaitForURLAsync("**/PatientPortal/*", new PageWaitForURLOptions { Timeout = 30000 });
    }

    private static async Task AssertFlashSuccessAsync(IPage page, string expectedContains)
    {
        var flash = page.Locator("#medflow-flash");
        // Puede existir en DOM como hidden (por estilos/toastr). Lo importante es el atributo.
        await flash.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 30000 });
        var dataSuccess = await flash.GetAttributeAsync("data-success");
        Assert.False(string.IsNullOrWhiteSpace(dataSuccess));
        Assert.Contains(expectedContains, dataSuccess!, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task AssertValidationVisibleAsync(IPage page)
    {
        // Múltiples pantallas usan validación con `div.text-danger` o `div.alert.alert-danger`,
        // pero en algunos casos hay elementos `.text-danger` ocultos (p.ej. dropdown).
        var anyDanger = page.Locator(".text-danger:visible, .alert-danger:visible, div.alert.alert-danger:visible, .validation-summary-errors:visible").First;
        await anyDanger.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 20000 });
        var txt = (await anyDanger.InnerTextAsync()).Trim();
        Assert.False(string.IsNullOrWhiteSpace(txt));
    }

    [Fact]
    public async Task Admin_CanCreateEditSaveCancelUser_WithValidation()
    {
        var page = await NewPageAsync();
        try
        {
            var adminEmail = "qa.admin@medflow.local";
            var createdEmail = $"qa.e2e.admin.{DateTime.UtcNow:yyyyMMddHHmmss}@medflow.local";

            await LoginStaffAsync(page, adminEmail);
            await page.GotoAsync("/AdminUsers");
            await page.GetByRole(AriaRole.Link, new() { Name = "Nuevo usuario" }).ClickAsync(new() { Force = true });

            // NEGATIVO: email inválido
            await page.Locator("input[name='Email']").FillAsync("bad-email");
            await page.Locator("input[name='Password']").FillAsync(QaPassword);
            await page.Locator("input[name='FirstName']").FillAsync("QA");
            await page.Locator("input[name='LastName']").FillAsync("AdminE2E");
            // Bootstrap custom-control: el label intercepta clicks; hay que hacer clic en el label
            await page.Locator("label[for='role_Reception']").ClickAsync(new() { Force = true });
            await page.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync(new() { Force = true });
            await AssertValidationVisibleAsync(page);
            Assert.Contains("/AdminUsers/Create", page.Url, StringComparison.OrdinalIgnoreCase);

            // POSITIVO: creación real
            await page.Locator("input[name='Email']").FillAsync(createdEmail);
            await page.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync(new() { Force = true });
            await page.WaitForURLAsync("**/AdminUsers", new PageWaitForURLOptions { Timeout = 30000 });
            await AssertFlashSuccessAsync(page, "Usuario creado correctamente");

            // EDITAR: abrir editor desde la tabla
            var row = page.Locator($"#tblUsers tbody tr:has-text('{createdEmail}')").First;
            await row.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 15000 });
            await row.Locator("a[title='Editar']").ClickAsync(new() { Force = true });
            await page.WaitForURLAsync("**/AdminUsers/Edit/*", new PageWaitForURLOptions { Timeout = 30000 });

            // NEGATIVO: email inválido en edición
            await page.Locator("input[name='Email']").FillAsync("bad-email-2");
            await page.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync(new() { Force = true });
            await AssertValidationVisibleAsync(page);

            // CANCELAR: regresar sin guardar
            await page.GetByRole(AriaRole.Link, new() { Name = "Cancelar" }).ClickAsync(new() { Force = true });
            await page.WaitForURLAsync("**/AdminUsers/Details/*", new PageWaitForURLOptions { Timeout = 30000 });

            // GUARDA válido: editar nuevamente y guardar cambios reales (teléfono)
            var detailsUrl = page.Url;
            await page.GetByRole(AriaRole.Link, new() { Name = "Editar" }).ClickAsync(new() { Force = true });
            await page.WaitForURLAsync("**/AdminUsers/Edit/*", new PageWaitForURLOptions { Timeout = 30000 });
            await page.Locator("input[name='Email']").FillAsync(createdEmail);
            await page.Locator("input[name='PhoneNumber']").FillAsync("555123456");
            await page.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync(new() { Force = true });
            await page.WaitForURLAsync("**/AdminUsers/Details/*", new PageWaitForURLOptions { Timeout = 30000 });
            Assert.Contains("/AdminUsers/Details/", page.Url, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await page.Context.Browser.CloseAsync();
        }
    }

    [Fact]
    public async Task Reception_CanCreateEditSaveCancelAppointment_WithValidation()
    {
        var page = await NewPageAsync();
        try
        {
            var email = "qa.reception@medflow.local";
            var reason = "QA-APPT-RECEP-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            await LoginStaffAsync(page, email);
            await page.GotoAsync("/Appointments/Create");

            // Negativo: paciente requerido
            await page.Locator("select[name='PatientId']").SelectOptionAsync(new[] { new SelectOptionValue { Value = "" } });
            await page.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync(new() { Force = true });
            await AssertValidationVisibleAsync(page);

            // Positivo: set de selects (primer paciente/doctor no vacío)
            var patientId = await GetFirstNonEmptySelectOptionValueAsync(page, "select[name='PatientId']");
            var doctorId = await GetFirstNonEmptySelectOptionValueAsync(page, "select[name='DoctorId']");
            Assert.False(string.IsNullOrWhiteSpace(patientId));
            Assert.False(string.IsNullOrWhiteSpace(doctorId));

            await page.Locator("select[name='PatientId']").SelectOptionAsync(patientId);
            await page.Locator("select[name='DoctorId']").SelectOptionAsync(doctorId);

            // Fecha/hora única por ejecución para evitar conflictos de horario con runs anteriores
            var apptTicks = DateTime.UtcNow.Ticks;
            var apptDaysAhead = 2 + (int)(apptTicks % 5); // 2-6 días adelante (en rango Today..Today+7)
            var apptHour = 8 + (int)((apptTicks / (TimeSpan.TicksPerSecond * 10)) % 8); // 8-15h, cambia cada 10s
            var today = DateTime.Today.AddDays(apptDaysAhead);
            var startTime = $"{apptHour:D2}:00";
            var endTime = $"{apptHour:D2}:30";
            await page.Locator("input[name='ScheduledDate']").FillAsync(today.ToString("yyyy-MM-dd"));
            await page.Locator("input[name='StartTime']").FillAsync(startTime);
            await page.Locator("input[name='EndTime']").FillAsync(endTime);
            await page.Locator("input[name='ConsultationRoom']").FillAsync("R-101");
            await page.Locator("select[name='Status']").SelectOptionAsync("0"); // Scheduled
            await page.Locator("textarea[name='Reason']").FillAsync(reason);
            await page.Locator("textarea[name='Notes']").FillAsync("Notas QA Reception");

            await page.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync(new() { Force = true });
            await AssertFlashSuccessAsync(page, "Cita agendada correctamente");

            // Ubicar fila por reason y entrar a detalles
            var row = page.Locator($"#tblAppointments tbody tr:has-text('{reason}')").First;
            await row.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 15000 });
            await row.Locator("a[title='Editar']").ClickAsync(new() { Force = true });
            await page.WaitForURLAsync("**/Appointments/Edit/*", new PageWaitForURLOptions { Timeout = 30000 });

            // Editar consultorio + guardar
            await page.Locator("input[name='ConsultationRoom']").FillAsync("R-202");
            await page.GetByRole(AriaRole.Button, new() { Name = "Guardar cambios" }).ClickAsync(new() { Force = true });
            await AssertFlashSuccessAsync(page, "Cita actualizada correctamente");

            // Confirmar persistencia en detalles
            row = page.Locator($"#tblAppointments tbody tr:has-text('{reason}')").First;
            await row.Locator("a[title='Ver']").ClickAsync(new() { Force = true });
            await page.WaitForURLAsync("**/Appointments/Details/*", new PageWaitForURLOptions { Timeout = 30000 });
            await page.Locator("text=R-202").WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });

            // CANCELAR: editar pero cancelar sin guardar (nota)
            await page.GetByRole(AriaRole.Link, new() { Name = "Editar" }).ClickAsync(new() { Force = true });
            await page.WaitForURLAsync("**/Appointments/Edit/*", new PageWaitForURLOptions { Timeout = 30000 });
            await page.Locator("textarea[name='Notes']").FillAsync("Notas NO guardar");
            await page.GetByRole(AriaRole.Link, new() { Name = "Cancelar" }).ClickAsync(new() { Force = true });
            await page.Locator("#tblAppointments").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30000 });

            row = page.Locator($"#tblAppointments tbody tr:has-text('{reason}')").First;
            await row.Locator("a[title='Ver']").ClickAsync(new() { Force = true });
            await page.WaitForURLAsync("**/Appointments/Details/*", new PageWaitForURLOptions { Timeout = 30000 });
            Assert.DoesNotContain("Notas NO guardar", await page.ContentAsync());

            // Cancelar cita: delete
            await page.GotoAsync("/Appointments");
            var row2 = page.Locator($"#tblAppointments tbody tr:has-text('{reason}')").First;
            await row2.Locator("form[data-confirm-delete] button[title='Eliminar']").ClickAsync(new() { Force = true });
            // SweetAlert confirmation (no es window.confirm)
            await page.Locator("button.swal2-confirm:has-text('Sí, eliminar'), button:has-text('Sí, eliminar')").First
                .ClickAsync(new() { Force = true });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Assert.False(await page.Locator($"#tblAppointments tbody tr:has-text('{reason}')").IsVisibleAsync());
        }
        finally
        {
            await page.Context.Browser.CloseAsync();
        }
    }

    [Fact]
    public async Task Doctor_CanCreateEditSaveMedicalRecord_WithValidation_AndRestrictions()
    {
        var page = await NewPageAsync();
        try
        {
            var email = "qa.doctor@medflow.local";
            var chief = "QA-DOC-CC-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var diagnosis = "QA-DOC-DX-OK-" + Guid.NewGuid().ToString("N")[..6];

            await LoginStaffAsync(page, email);

            // Bypass negativo: Doctor no debe entrar a AdminUsers
            await page.GotoAsync("/AdminUsers");
            await page.Locator("h1:has-text('Acceso denegado'), h2.h4:has-text('Acceso denegado'), h2:has-text('Acceso denegado')").First
                .WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });

            // Entrar a historia clínica
            await page.GotoAsync("/MedicalRecords");
            var openHistory = page.Locator("a:has-text('Abrir historia')").First;
            await openHistory.ClickAsync(new() { Force = true });

            await page.GetByRole(AriaRole.Link, new() { Name = "Nueva consulta" }).ClickAsync(new() { Force = true });
            await page.WaitForURLAsync("**/MedicalRecords/Create*", new PageWaitForURLOptions { Timeout = 30000 });

            // NEGATIVO: Doctor requerido
            await page.Locator("select[name='DoctorId']").SelectOptionAsync(new[] { new SelectOptionValue { Value = "" } });
            await page.GetByRole(AriaRole.Button, new() { Name = "Guardar consulta" }).ClickAsync(new() { Force = true });
            await AssertValidationVisibleAsync(page);

            // POSITIVO: completar e ingresar
            var doctorId = await GetFirstNonEmptySelectOptionValueAsync(page, "select[name='DoctorId']");
            Assert.False(string.IsNullOrWhiteSpace(doctorId));
            await page.Locator("select[name='DoctorId']").SelectOptionAsync(doctorId);
            await page.Locator("textarea[name='ChiefComplaint']").FillAsync(chief);
            await page.Locator("textarea[name='Diagnosis']").FillAsync(diagnosis);

            // Prescripción: llenar la primera fila
            await page.Locator("input[name='PrescriptionLines[0].MedicationName']").FillAsync("Paracetamol");
            await page.Locator("input[name='PrescriptionLines[0].Dosage']").FillAsync("500mg");
            await page.Locator("input[name='PrescriptionLines[0].Frequency']").FillAsync("cada 8h");
            await page.Locator("input[name='PrescriptionLines[0].Duration']").FillAsync("5 días");
            await page.Locator("input[name='PrescriptionLines[0].Instructions']").FillAsync("Con comida");

            await page.Locator("button:has-text('Guardar consulta')").ClickAsync(new() { Force = true });
            await AssertFlashSuccessAsync(page, "Consulta registrada correctamente");
            await page.Locator("a:has-text('Nueva consulta')").First.WaitForAsync(new LocatorWaitForOptions { Timeout = 30000 });

            // Editar el registro creado
            var card = page.Locator($"div.timeline-card:has-text('{chief}')").First;
            await card.Locator("a:has-text('Editar')").ClickAsync(new() { Force = true });
            await page.WaitForURLAsync("**/MedicalRecords/Edit/*", new PageWaitForURLOptions { Timeout = 30000 });

            // NEGATIVO: enviar inválido y verificar validación
            await page.Locator("select[name='DoctorId']").SelectOptionAsync(new[] { new SelectOptionValue { Value = "" } });
            await page.Locator("button:has-text('Guardar cambios')").ClickAsync(new() { Force = true });
            await AssertValidationVisibleAsync(page);

            // CANCELAR: volver al detalle sin guardar
            await page.GetByRole(AriaRole.Link, new() { Name = "Cancelar" }).ClickAsync(new() { Force = true });
            await page.WaitForURLAsync("**/MedicalRecords/Details/*", new PageWaitForURLOptions { Timeout = 30000 });

            // GUARDA: abrir editar y guardar diagnóstico actualizado
            await page.GetByRole(AriaRole.Link, new() { Name = "Editar" }).ClickAsync(new() { Force = true });
            await page.WaitForURLAsync("**/MedicalRecords/Edit/*", new PageWaitForURLOptions { Timeout = 30000 });
            doctorId = await GetFirstNonEmptySelectOptionValueAsync(page, "select[name='DoctorId']");
            await page.Locator("select[name='DoctorId']").SelectOptionAsync(doctorId!);
            var dx2 = diagnosis + "-2";
            await page.Locator("textarea[name='Diagnosis']").FillAsync(dx2);
            await page.Locator("button:has-text('Guardar cambios')").ClickAsync(new() { Force = true });

            await page.WaitForURLAsync("**/MedicalRecords/Details/*", new PageWaitForURLOptions { Timeout = 30000 });
            await page.Locator($"text={dx2}").WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });
        }
        finally
        {
            await page.Context.Browser.CloseAsync();
        }
    }

    [Fact]
    public async Task Billing_CanCreateInvoice_RegisterPayment_CancelPayment_WithValidation()
    {
        var page = await NewPageAsync();
        try
        {
            var email = "qa.billing@medflow.local";
            var lineDesc = "QA-BILL-LINE-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var uniqueRef = "QA-PAY-REF-" + Guid.NewGuid().ToString("N")[..6];

            await LoginStaffAsync(page, email);
            await page.GotoAsync("/BillingInvoices");
            await page.GetByRole(AriaRole.Link, new() { Name = "Nueva factura" }).ClickAsync(new() { Force = true });

            // Negativo: línea sin descripción requerida
            await page.Locator("input[name='Lines[0].Description']").FillAsync("");
            var patientId = await GetFirstNonEmptySelectOptionValueAsync(page, "select[name='PatientId']");
            Assert.False(string.IsNullOrWhiteSpace(patientId));
            await page.Locator("select[name='PatientId']").SelectOptionAsync(patientId);
            await page.Locator("input[name='Lines[0].Quantity']").FillAsync("1");
            await page.Locator("input[name='Lines[0].UnitPrice']").FillAsync("100");
            await page.Locator("input[name='Lines[0].LineDiscount']").FillAsync("0");
            await page.Locator("button:has-text('Guardar factura')").ClickAsync(new() { Force = true });
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Assert.DoesNotContain("/BillingInvoices/Details/", page.Url, StringComparison.OrdinalIgnoreCase);

            var frm = page.Locator("#frmBill").First;
            await frm.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 15000 });
            var content = await page.ContentAsync();
            Assert.Contains("Descripción", content, StringComparison.OrdinalIgnoreCase);

            // Positivo: descripción y monto
            await page.Locator("input[name='Lines[0].Description']").FillAsync(lineDesc);
            await page.Locator("button:has-text('Guardar factura')").ClickAsync(new() { Force = true });
            await page.WaitForURLAsync("**/BillingInvoices/Details/*", new PageWaitForURLOptions { Timeout = 30000 });

            // Guardar URL de detalles de la factura
            var invoiceDetailsUrl = page.Url;

            // NEGATIVO: amount=0 — cargar Details fresh (CSRF token fresco) antes de cada submit
            await page.GotoAsync(invoiceDetailsUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.Locator("input[name='Amount']").FillAsync("0");
            await page.Locator("input[name='ReferenceNumber']").FillAsync(uniqueRef);
            await page.RunAndWaitForNavigationAsync(async () =>
            {
                await page.Locator("button:has-text('Registrar pago')").ClickAsync(new() { Force = true });
            }, new PageRunAndWaitForNavigationOptions { WaitUntil = WaitUntilState.Load, Timeout = 15000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Assert.True(page.Url.Contains("/BillingInvoices/Details/", StringComparison.OrdinalIgnoreCase),
                $"Negative submit should redirect to Details. Got: {page.Url}");
            Assert.False(await page.Locator("button.btn-cancel-pay").IsVisibleAsync(),
                "No debe haber btn-cancel-pay: el pago de $0 fue rechazado.");

            // POSITIVO: amount=$50 — cargar Details fresh para CSRF token limpio
            await page.GotoAsync(invoiceDetailsUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.Locator("input[name='Amount']").FillAsync("50");
            await page.Locator("input[name='ReferenceNumber']").FillAsync(uniqueRef + "-ok");
            await page.RunAndWaitForNavigationAsync(async () =>
            {
                await page.Locator("button:has-text('Registrar pago')").ClickAsync(new() { Force = true });
            }, new PageRunAndWaitForNavigationOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Assert.True(page.Url.Contains("/BillingInvoices/Details/", StringComparison.OrdinalIgnoreCase),
                $"Expected Details after positive submit. Got: {page.Url}");
            // Esperar que la fila con el número de referencia aparezca en la tabla de pagos
            await page.Locator($"td:has-text('{uniqueRef}-ok')").WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });
            // btn-cancel-pay confirma que el pago fue registrado
            await page.Locator("button.btn-cancel-pay").First.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000, State = WaitForSelectorState.Attached });

            // Cancelar pago con SweetAlert confirm
            await page.Locator("button.btn-cancel-pay").ClickAsync(new() { Force = true });
            await page.Locator("button.swal2-confirm").First.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
            await page.Locator("button.swal2-confirm").First.ClickAsync(new() { Force = true });
            // CancelPayment → 302 → Details (URLs distintas)
            await page.WaitForURLAsync("**/BillingInvoices/Details/*", new PageWaitForURLOptions { Timeout = 30000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            // Verificar que el pago fue anulado: btn-cancel-pay ya no debe aparecer
            Assert.Equal(0, await page.Locator("button.btn-cancel-pay").CountAsync());
        }
        finally
        {
            await page.Context.Browser.CloseAsync();
        }
    }

    [Fact]
    public async Task Staff_CanCreateEditSaveCancelAppointment_And_IsRestrictedFromClinicalBilling()
    {
        var page = await NewPageAsync();
        try
        {
            var email = "qa.staff@medflow.local";
            var reason = "QA-APPT-STAFF-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            await LoginStaffAsync(page, email);

            // Bypass negativo: staff no debe acceder a clinical/billing
            await page.GotoAsync("/MedicalRecords");
            await page.Locator("h1:has-text('Acceso denegado'), h2.h4:has-text('Acceso denegado')").First
                .WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });

            await page.GotoAsync("/BillingInvoices");
            await page.Locator("h1:has-text('Acceso denegado'), h2.h4:has-text('Acceso denegado')").First
                .WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });

            // Crear cita
            await page.GotoAsync("/Appointments/Create");
            var patientId = await GetFirstNonEmptySelectOptionValueAsync(page, "select[name='PatientId']");
            var doctorId = await GetFirstNonEmptySelectOptionValueAsync(page, "select[name='DoctorId']");
            Assert.False(string.IsNullOrWhiteSpace(patientId));
            Assert.False(string.IsNullOrWhiteSpace(doctorId));
            await page.Locator("select[name='PatientId']").SelectOptionAsync(patientId);
            await page.Locator("select[name='DoctorId']").SelectOptionAsync(doctorId);
            var staffTicks = DateTime.UtcNow.Ticks;
            var staffDaysAhead = 1 + (int)(staffTicks % 4); // 1-4 días (en rango Today..Today+7)
            var staffHour = 10 + (int)((staffTicks / (TimeSpan.TicksPerSecond * 10)) % 6); // 10-15h
            var today = DateTime.Today.AddDays(staffDaysAhead);
            await page.Locator("input[name='ScheduledDate']").FillAsync(today.ToString("yyyy-MM-dd"));
            await page.Locator("input[name='StartTime']").FillAsync($"{staffHour:D2}:00");
            await page.Locator("input[name='EndTime']").FillAsync($"{staffHour:D2}:30");
            await page.Locator("input[name='ConsultationRoom']").FillAsync("S-1");
            await page.Locator("select[name='Status']").SelectOptionAsync("0");
            await page.Locator("textarea[name='Reason']").FillAsync(reason);
            await page.Locator("textarea[name='Notes']").FillAsync("Notas staff");
            await page.Locator("button:has-text('Guardar')").ClickAsync(new() { Force = true });

            await page.WaitForURLAsync("**/Appointments", new PageWaitForURLOptions { Timeout = 30000 });
            var row = page.Locator($"#tblAppointments tbody tr:has-text('{reason}')").First;
            await row.WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });

            // Editar + guardar
            await row.Locator("a[title='Editar']").ClickAsync(new() { Force = true });
            await page.Locator("input[name='ConsultationRoom']").FillAsync("S-2");
            await page.Locator("button:has-text('Guardar cambios')").ClickAsync(new() { Force = true });
            await page.WaitForURLAsync("**/Appointments", new PageWaitForURLOptions { Timeout = 30000 });

            // Confirmar en detalles
            row = page.Locator($"#tblAppointments tbody tr:has-text('{reason}')").First;
            await row.Locator("a[title='Ver']").ClickAsync(new() { Force = true });
            await page.WaitForURLAsync("**/Appointments/Details/*", new PageWaitForURLOptions { Timeout = 30000 });
            await page.Locator("text=S-2").WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });

            // Cancelar delete
            await page.GotoAsync("/Appointments");
            row = page.Locator($"#tblAppointments tbody tr:has-text('{reason}')").First;
            await row.Locator("form[data-confirm-delete] button[title='Eliminar']").ClickAsync(new() { Force = true });
            await page.Locator("button.swal2-confirm:has-text('Sí, eliminar'), button:has-text('Sí, eliminar')").First
                .ClickAsync(new() { Force = true });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Assert.False(await page.Locator($"#tblAppointments tbody tr:has-text('{reason}')").IsVisibleAsync());
        }
        finally
        {
            await page.Context.Browser.CloseAsync();
        }
    }

    [Fact]
    public async Task Patient_Portal_CanViewProfile_And_BypassAttempts_AreBlocked()
    {
        var page = await NewPageAsync();
        try
        {
            var email = "qa.patient@medflow.local";
            await LoginPatientAsync(page, email);

            // Ver perfil
            await page.GotoAsync("/PatientPortal/perfil");
            await page.GetByRole(AriaRole.Heading, new() { Name = "Mi Perfil" }).WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });

            // Intento de bypass a admin
            await page.GotoAsync("/AdminUsers");
            await page.Locator("h1:has-text('Acceso denegado'), h2.h4:has-text('Acceso denegado')").First
                .WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });
            await page.Locator("text=Volver al portal del paciente").WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });

            // Si edición está permitida, intentar guardar inválido y validar que NO se persiste.
            var saveBtnCount = await page.GetByRole(AriaRole.Button, new() { Name = "Guardar cambios" }).CountAsync();
            if (saveBtnCount > 0)
            {
                await page.GotoAsync("/PatientPortal/perfil");
                var originalTelefono = await page.Locator("input[name='Telefono']").GetAttributeAsync("value");
                var originalCorreo = await page.Locator("input[name='Correo']").GetAttributeAsync("value");
                await page.Locator("input[name='Telefono']").FillAsync("bad-phone");
                await page.Locator("input[name='Correo']").FillAsync("bad-email");
                await page.GetByRole(AriaRole.Button, new() { Name = "Guardar cambios" }).ClickAsync(new() { Force = true });

                // Debe permanecer sin éxito (no medflow-flash success) y valores no deben persistir.
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                var telefonoAfter = await page.Locator("input[name='Telefono']").GetAttributeAsync("value");
                var correoAfter = await page.Locator("input[name='Correo']").GetAttributeAsync("value");
                Assert.Equal(originalTelefono, telefonoAfter);
                Assert.Equal(originalCorreo, correoAfter);
            }
        }
        finally
        {
            await page.Context.Browser.CloseAsync();
        }
    }
}

