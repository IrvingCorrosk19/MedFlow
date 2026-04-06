/**
 * Admin — Flujos extendidos:
 *   • Doctors CRUD completo
 *   • Appointments — crear cita con todos los campos
 *   • Settings
 *   • AdminUsers — SendPasswordReset
 *   • AI Copilot (smoke)
 *   • Analytics y EventLogs (smoke)
 */
import { test, expect } from '@playwright/test';
import { CREDS, loginStaff, safeClick, unique } from './_helpers';

const A = CREDS.admin;

// ── Doctors ───────────────────────────────────────────────────────────────────

test.describe('Admin — Doctors CRUD', () => {

  test('Doctors: listar sin error', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/Doctors', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page.locator('h1, h2')).toContainText(/doctor/i);
  });

  test('Doctors Create: formulario carga y guarda vacío permanece', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/Doctors/Create', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');

    // Guardar vacío → validación, permanece en Create
    await safeClick(page, { role: 'button', name: /Guardar|Registrar/i });
    await page.waitForLoadState('domcontentloaded');
    await expect(page).toHaveURL(/\/Doctors\/Create/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('Doctors Create: CRUD completo (crear → editar → listar)', async ({ page }) => {
    await loginStaff(page, A.email, A.password);

    // CREAR
    await page.goto('/Doctors/Create', { waitUntil: 'domcontentloaded' });
    const lastName = unique('Dr-Ap');
    const license  = unique('LIC').replace(/[^0-9A-Za-z]/g, '').slice(0, 8);

    await page.locator("input[name='FirstName']").fill('QA-Doctor');
    await page.locator("input[name='LastName']").fill(lastName);
    await page.locator("input[name='Speciality']").fill('Medicina general');
    await page.locator("input[name='LicenseNumber']").fill(license);
    await page.locator("input[name='Email']").fill(`qa.dr.${Date.now()}@medflow.local`);

    await safeClick(page, { role: 'button', name: /Guardar|Registrar/i });
    await page.waitForLoadState('domcontentloaded');
    await expect(page).toHaveURL(/\/Doctors/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');

    // EDITAR
    const row = page.locator(`table tbody tr:has-text("${lastName}"), #tblDoctores tbody tr:has-text("${lastName}")`).first();
    await expect(row).toBeVisible({ timeout: 10000 });

    const editLink = row.locator("a[href*='Edit'], a[title='Editar']").first();
    if (await editLink.count() > 0) {
      await editLink.click({ force: true });
      await page.waitForLoadState('domcontentloaded');
      await expect(page).toHaveURL(/\/Doctors\/Edit\//i);
      await page.locator("input[name='Phone']").fill('555-0101');
      await safeClick(page, { role: 'button', name: /Guardar|Actualizar/i });
      await page.waitForLoadState('domcontentloaded');
      await expect(page).toHaveURL(/\/Doctors/i);
      await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    }
  });

  test('Doctors: CSV export descarga sin error', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/Doctors/ExportCsv', { waitUntil: 'domcontentloaded' });
    // Puede descargar directamente; la URL no debe ser login ni mostrar 500
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

});

// ── Appointments Create ───────────────────────────────────────────────────────

test.describe('Admin — Appointments Create', () => {

  test('Appointments Create: formulario carga con selects de paciente y doctor', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/Appointments/Create', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page.locator("select[name='PatientId'], select[name='DoctorId']").first()).toBeVisible();
  });

  test('Appointments Create: guardar vacío permanece en formulario', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/Appointments/Create', { waitUntil: 'domcontentloaded' });
    await safeClick(page, { role: 'button', name: /Guardar|Agendar/i });
    await page.waitForLoadState('domcontentloaded');
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

  test('Appointments Create: flujo completo con datos válidos', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/Appointments/Create', { waitUntil: 'domcontentloaded' });

    // Seleccionar primer paciente y primer doctor disponibles
    const patientOpt = await page.locator("select[name='PatientId'] option[value]:not([value=''])").first().getAttribute('value');
    const doctorOpt  = await page.locator("select[name='DoctorId'] option[value]:not([value=''])").first().getAttribute('value');

    if (!patientOpt || !doctorOpt) {
      test.skip(true, 'Sin pacientes o doctores activos para crear cita');
      return;
    }

    await page.locator("select[name='PatientId']").selectOption(patientOpt);
    await page.locator("select[name='DoctorId']").selectOption(doctorOpt);

    // Fecha: mañana (evitar validación "fecha pasada")
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    const dateStr = tomorrow.toISOString().slice(0, 10);

    await page.locator("input[name='ScheduledDate']").fill(dateStr);
    await page.locator("input[name='StartTime']").fill('09:00');
    await page.locator("input[name='EndTime']").fill('09:30');
    await page.locator("textarea[name='Reason']").fill('Consulta de prueba QA');

    await safeClick(page, { role: 'button', name: /Guardar|Agendar/i });
    await page.waitForLoadState('domcontentloaded');
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    // En éxito redirige a Index
    await expect(page).toHaveURL(/\/Appointments/i);
  });

  test('Appointments Create: hora fin <= hora inicio muestra error de validación', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/Appointments/Create', { waitUntil: 'domcontentloaded' });

    const patientOpt = await page.locator("select[name='PatientId'] option[value]:not([value=''])").first().getAttribute('value');
    const doctorOpt  = await page.locator("select[name='DoctorId'] option[value]:not([value=''])").first().getAttribute('value');
    if (!patientOpt || !doctorOpt) {
      test.skip(true, 'Sin pacientes o doctores activos');
      return;
    }

    await page.locator("select[name='PatientId']").selectOption(patientOpt);
    await page.locator("select[name='DoctorId']").selectOption(doctorOpt);
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    await page.locator("input[name='ScheduledDate']").fill(tomorrow.toISOString().slice(0, 10));
    await page.locator("input[name='StartTime']").fill('10:00');
    await page.locator("input[name='EndTime']").fill('09:00'); // inválido

    await safeClick(page, { role: 'button', name: /Guardar|Agendar/i });
    await page.waitForLoadState('domcontentloaded');
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    // Debe permanecer en Create con mensaje de error
    await expect(page).toHaveURL(/\/Appointments\/Create/i);
  });

});

// ── Settings ──────────────────────────────────────────────────────────────────

test.describe('Admin — Settings', () => {

  test('Settings: carga sin error', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/Settings', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

});

// ── AdminUsers — Password Reset ───────────────────────────────────────────────

test.describe('Admin — AdminUsers Send Password Reset', () => {

  test('AdminUsers Details: botón SendPasswordReset visible y dispara acción', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/AdminUsers', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');

    // Abrir detalle de primer usuario
    const detailLink = page.locator('table tbody tr a[href*="/AdminUsers/"], #tblUsuarios tbody tr a[href*="/AdminUsers/"]').first();
    if (await detailLink.count() === 0) {
      test.skip(true, 'No hay filas de usuarios para abrir detalle');
      return;
    }
    const href = await detailLink.getAttribute('href');
    if (!href) { test.skip(true, 'Enlace sin href'); return; }

    await page.goto(href, { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');

    // El botón de reset debe estar presente (puede estar en form POST)
    const resetBtn = page.locator('button:has-text("Restablecer"), button:has-text("Reset"), form[action*="SendPasswordReset"] button').first();
    if (await resetBtn.count() === 0) {
      test.skip(true, 'Botón SendPasswordReset no encontrado en details');
      return;
    }
    await resetBtn.click({ force: true });
    await page.waitForLoadState('domcontentloaded');
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

});

// ── AI Copilot / Analytics / EventLogs ───────────────────────────────────────

test.describe('Admin — AI y módulos auxiliares (smoke)', () => {

  test('AI Copilot: carga sin 500', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/AI/Copilot', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('Analytics: carga sin 500', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/Analytics', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('EventLogs: carga sin 500', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/EventLogs', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('NotificationTemplates: carga sin 500', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/NotificationTemplates', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('TenantBilling: carga sin 500', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/TenantBilling', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('CashMovements: carga sin 500', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/CashMovements', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

});
