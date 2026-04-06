/**
 * QA Admin (qa.admin@medflow.local) — mismo nivel de permisos que Admin de clínica.
 * Verifica que el usuario seeded con rol Admin en el tenant demo tiene
 * acceso completo a todos los módulos de clínica.
 */
import { test, expect } from '@playwright/test';
import { CREDS, loginStaff, expectAccessDenied, safeClick, unique } from './_helpers';

const QA = CREDS.qaAdmin;

// ── Login y navegación ────────────────────────────────────────────────────────

test.describe('QA Admin — Login y módulos', () => {

  test('Login QA Admin exitoso', async ({ page }) => {
    await loginStaff(page, QA.email, QA.password);
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('QA Admin: navegación completa de módulos', async ({ page }) => {
    await loginStaff(page, QA.email, QA.password);

    const modules = [
      '/Dashboard',
      '/Patients',
      '/Appointments',
      '/Doctors',
      '/AdminUsers',
      '/AdminRoles',
      '/BillingInvoices',
      '/MedicalRecords',
      '/Reports/Appointments',
      '/Reports/Patients',
      '/Reports/Financial',
      '/Reports/Doctors',
      '/Settings',
    ];

    for (const mod of modules) {
      await page.goto(mod, { waitUntil: 'domcontentloaded' });
      await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
      await expect(page).not.toHaveURL(/\/Account\/Login/i);
    }
  });

});

// ── Operaciones clave ─────────────────────────────────────────────────────────

test.describe('QA Admin — Pacientes CRUD', () => {

  test('Pacientes: crear nuevo paciente', async ({ page }) => {
    await loginStaff(page, QA.email, QA.password);
    await page.goto('/Patients/Create', { waitUntil: 'domcontentloaded' });

    const lastName = unique('QAAdm-Ap');
    const doc      = unique('QA').replace(/[^0-9A-Za-z]/g, '').slice(0, 10);

    await page.locator("input[name='PrimerNombre']").fill('QA-Admin2');
    await page.locator("input[name='PrimerApellido']").fill(lastName);
    await page.locator("input[name='FechaNacimiento']").fill('1995-07-10');

    const sexo = await page.locator("select[name='Sexo'] option[value]:not([value=''])").first().getAttribute('value');
    const tipo = await page.locator("select[name='TipoDocumento'] option[value]:not([value=''])").first().getAttribute('value');
    if (sexo) await page.locator("select[name='Sexo']").selectOption(sexo);
    if (tipo) await page.locator("select[name='TipoDocumento']").selectOption(tipo);
    await page.locator("input[name='NumeroDocumento']").fill(doc);
    await page.locator("input[name='Correo']").fill(`qa.admin2.${Date.now()}@medflow.local`);

    await safeClick(page, { role: 'button', name: 'Guardar paciente' });
    await page.waitForLoadState('domcontentloaded');
    await expect(page).toHaveURL(/\/Patients/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

});

test.describe('QA Admin — Doctors', () => {

  test('Doctors: listar y crear formulario sin error', async ({ page }) => {
    await loginStaff(page, QA.email, QA.password);
    await page.goto('/Doctors', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');

    await page.goto('/Doctors/Create', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page.locator("input[name='FirstName']")).toBeVisible();
  });

});

test.describe('QA Admin — Usuarios y roles', () => {

  test('AdminUsers: listar sin error', async ({ page }) => {
    await loginStaff(page, QA.email, QA.password);
    await page.goto('/AdminUsers', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('h1, h2')).toContainText(/Usuarios/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('AdminRoles: listar sin error', async ({ page }) => {
    await loginStaff(page, QA.email, QA.password);
    await page.goto('/AdminRoles', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('h1, h2')).toContainText(/Roles/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

});

test.describe('QA Admin — Reportes', () => {

  test('Reportes: todos cargan sin 500', async ({ page }) => {
    await loginStaff(page, QA.email, QA.password);
    for (const r of ['/Reports/Appointments', '/Reports/Patients', '/Reports/Financial', '/Reports/Doctors']) {
      await page.goto(r, { waitUntil: 'domcontentloaded' });
      await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
      await expect(page).not.toHaveURL(/\/Account\/Login/i);
    }
  });

});

// ── Restricciones ─────────────────────────────────────────────────────────────

test.describe('QA Admin — Acceso denegado SuperAdmin Area', () => {

  test('/SuperAdmin/Tenants denegado', async ({ page }) => {
    await loginStaff(page, QA.email, QA.password);
    await page.goto('/SuperAdmin/Tenants', { waitUntil: 'domcontentloaded' });
    await expectAccessDenied(page);
  });

  test('/SuperAdmin/Plans denegado', async ({ page }) => {
    await loginStaff(page, QA.email, QA.password);
    await page.goto('/SuperAdmin/Plans', { waitUntil: 'domcontentloaded' });
    await expectAccessDenied(page);
  });

});
