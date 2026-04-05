import { test, expect } from '@playwright/test';
import { CREDS, loginStaff, safeClick, unique } from './_helpers';

const A = CREDS.admin;

test.describe('Admin — Flujo completo', () => {

  test('FASE 2: Login Admin', async ({ page }) => {
    await page.goto('/Account/Login', { waitUntil: 'domcontentloaded' });
    await page.locator("input[name='Email']").fill(A.email);
    await page.locator("input[name='Password']").fill(A.password);
    await page.getByRole('button', { name: 'Entrar' }).click({ force: true });
    await page.waitForLoadState('domcontentloaded');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500');
  });

  test('FASE 3: Navegación de módulos Admin', async ({ page }) => {
    await loginStaff(page, A.email, A.password);

    const modules = [
      '/Dashboard',
      '/Patients',
      '/Appointments',
      '/AdminUsers',
      '/AdminRoles',
      '/BillingInvoices',
      '/Reports/Appointments',
      '/Reports/Patients',
      '/Reports/Financial',
      '/Reports/Doctors',
    ];

    for (const mod of modules) {
      await page.goto(mod, { waitUntil: 'domcontentloaded' });
      await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
      await expect(page).not.toHaveURL(/\/Account\/Login/i);
    }
  });

  test('FASE 4-A: Pacientes — CRUD completo', async ({ page }) => {
    await loginStaff(page, A.email, A.password);

    // CREAR
    await page.goto('/Patients', { waitUntil: 'domcontentloaded' });
    await safeClick(page, { role: 'link', name: 'Nuevo paciente' });
    await expect(page).toHaveURL(/\/Patients\/Create/i);

    // NEGATIVO: guardar vacío
    await safeClick(page, { role: 'button', name: 'Guardar paciente' });
    await expect(page).toHaveURL(/\/Patients\/Create/i);

    const lastName = unique('ADM-Ap');
    const doc = unique('AD').replace(/[^0-9A-Za-z]/g, '').slice(0, 10);
    await page.locator("input[name='PrimerNombre']").fill('QA-Admin');
    await page.locator("input[name='PrimerApellido']").fill(lastName);
    await page.locator("input[name='FechaNacimiento']").fill('1988-03-20');

    const sexo = await page.locator("select[name='Sexo'] option[value]:not([value=''])").first().getAttribute('value');
    const tipo = await page.locator("select[name='TipoDocumento'] option[value]:not([value=''])").first().getAttribute('value');
    if (sexo) await page.locator("select[name='Sexo']").selectOption(sexo);
    if (tipo) await page.locator("select[name='TipoDocumento']").selectOption(tipo);
    await page.locator("input[name='NumeroDocumento']").fill(doc);
    await page.locator("input[name='Correo']").fill(`qa.admin.${Date.now()}@medflow.local`);

    await safeClick(page, { role: 'button', name: 'Guardar paciente' });
    await expect(page).toHaveURL(/\/Patients/i);

    // EDITAR
    const row = page.locator(`#tblPacientes tbody tr:has-text("${lastName}")`).first();
    await expect(row).toBeVisible({ timeout: 10000 });
    await row.locator("a[title='Editar']").click({ force: true });
    await expect(page).toHaveURL(/\/Patients\/Edit\//i);

    // CANCELAR
    await page.locator("input[name='Telefono']").fill('999-NOGUARDAR');
    await safeClick(page, { role: 'link', name: 'Volver al listado' });
    await expect(page).toHaveURL(/\/Patients/i);

    // GUARDAR cambio real
    const row2 = page.locator(`#tblPacientes tbody tr:has-text("${lastName}")`).first();
    await row2.locator("a[title='Editar']").click({ force: true });
    await page.locator("input[name='Telefono']").fill('555-9999');
    await safeClick(page, { role: 'button', name: 'Guardar cambios' });
    await expect(page).toHaveURL(/\/Patients/i);
  });

  test('FASE 4-B: Citas — listar y filtrar', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/Appointments', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);

    // Intentar nueva cita
    const newLink = page.getByRole('link', { name: /Nueva cita|Agendar/i });
    if (await newLink.count() > 0) {
      await newLink.first().click({ force: true });
      await page.waitForLoadState('domcontentloaded');
      await expect(page.locator('body')).not.toContainText('500');
      await page.goBack();
    }
  });

  test('FASE 4-C: Usuarios — listar', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/AdminUsers', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('h1, h2')).toContainText(/Usuarios/i);
    await expect(page.locator('body')).not.toContainText('500');
  });

  test('FASE 4-D: Roles — listar', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/AdminRoles', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('h1, h2')).toContainText(/Roles/i);
    await expect(page.locator('body')).not.toContainText('500');
  });

  test('FASE 4-E: Billing — listar', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/BillingInvoices', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

  test('FASE 4-F: Reportes — Citas, Pacientes, Financiero, Doctores', async ({ page }) => {
    await loginStaff(page, A.email, A.password);

    for (const r of ['/Reports/Appointments', '/Reports/Patients', '/Reports/Financial', '/Reports/Doctors']) {
      await page.goto(r, { waitUntil: 'domcontentloaded' });
      await expect(page.locator('body')).not.toContainText('500');
      await expect(page).not.toHaveURL(/\/Account\/Login/i);
    }
  });

  test('FASE 5: Validaciones — email inválido en paciente', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/Patients/Create', { waitUntil: 'domcontentloaded' });
    await page.locator("input[name='Correo']").fill('no-es-email');
    await safeClick(page, { role: 'button', name: 'Guardar paciente' });
    await expect(page).toHaveURL(/\/Patients\/Create/i);
    await expect(page.locator('body')).not.toContainText('500');
  });

  test('FASE 7: Regresión — login + dashboard + pacientes', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/Dashboard', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('h1')).toContainText(/Dashboard|Inicio/i);
    await page.goto('/Patients', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500');
  });

});
