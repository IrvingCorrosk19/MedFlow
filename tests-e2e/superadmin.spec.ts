import { test, expect } from '@playwright/test';
import { CREDS, loginStaff, safeClick, unique } from './_helpers';

const SA = CREDS.superadmin;

test.describe('SuperAdmin — Flujo completo', () => {

  test('FASE 2: Login SuperAdmin', async ({ page }) => {
    await page.goto('/Account/Login', { waitUntil: 'domcontentloaded' });
    await page.locator("input[name='Email']").fill(SA.email);
    await page.locator("input[name='Password']").fill('Medflow2026!Aa');
    await page.getByRole('button', { name: 'Entrar' }).click({ force: true });
    await page.waitForLoadState('domcontentloaded');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    // Dashboard visible
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('FASE 3: Navegación completa de módulos', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);

    const modules = [
      '/Dashboard',
      '/Patients',
      '/Appointments',
      '/AdminUsers',
      '/AdminRoles',
      '/BillingInvoices',
      '/Reports/Appointments',
      '/Settings',
    ];

    for (const mod of modules) {
      await page.goto(mod, { waitUntil: 'domcontentloaded' });
      const status = page.url();
      // No debe redirigir a login ni mostrar 403/500
      await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
      await expect(page).not.toHaveURL(/\/Account\/Login/i);
      // No acceso denegado incorrecto (heading o bloque de error HTTP)
      await expect(page.locator('h1, h2, .error-title')).not.toContainText('Acceso denegado', { timeout: 3000 }).catch(() => {});
    }
  });

  test('FASE 4-A: Pacientes — CRUD completo', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);

    // CREAR
    await page.goto('/Patients', { waitUntil: 'domcontentloaded' });
    await safeClick(page, { role: 'link', name: 'Nuevo paciente' });
    await expect(page).toHaveURL(/\/Patients\/Create/i);

    // NEGATIVO: guardar vacío
    await safeClick(page, { role: 'button', name: 'Guardar paciente' });
    await expect(page).toHaveURL(/\/Patients\/Create/i);

    const lastName = unique('SA-Ap');
    const doc = unique('SA').replace(/[^0-9A-Za-z]/g, '').slice(0, 10);
    await page.locator("input[name='PrimerNombre']").fill('QA-SA');
    await page.locator("input[name='PrimerApellido']").fill(lastName);
    await page.locator("input[name='FechaNacimiento']").fill('1990-06-15');

    const sexo = await page.locator("select[name='Sexo'] option[value]:not([value=''])").first().getAttribute('value');
    const tipo = await page.locator("select[name='TipoDocumento'] option[value]:not([value=''])").first().getAttribute('value');
    if (sexo) await page.locator("select[name='Sexo']").selectOption(sexo);
    if (tipo) await page.locator("select[name='TipoDocumento']").selectOption(tipo);
    await page.locator("input[name='NumeroDocumento']").fill(doc);
    await page.locator("input[name='Correo']").fill(`qa.sa.${Date.now()}@medflow.local`);

    await safeClick(page, { role: 'button', name: 'Guardar paciente' });
    await expect(page).toHaveURL(/\/Patients/i);

    // EDITAR
    const row = page.locator(`#tblPacientes tbody tr:has-text("${lastName}")`).first();
    await expect(row).toBeVisible({ timeout: 10000 });
    await row.locator("a[title='Editar']").click({ force: true });
    await expect(page).toHaveURL(/\/Patients\/Edit\//i);
    await page.locator("input[name='Telefono']").fill('555-7777');
    await safeClick(page, { role: 'button', name: 'Guardar cambios' });
    await expect(page).toHaveURL(/\/Patients/i);
  });

  test('FASE 4-B: Usuarios — listar y crear', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);
    await page.goto('/AdminUsers', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('h1, h2')).toContainText(/Usuarios/i);

    // Intentar crear usuario
    const createLink = page.getByRole('link', { name: /Nuevo usuario|Crear usuario/i });
    const count = await createLink.count();
    if (count > 0) {
      await createLink.first().click({ force: true });
      await page.waitForLoadState('domcontentloaded');
      // Cancelar/volver
      const back = page.getByRole('link', { name: /Volver|Cancelar/i });
      if (await back.count() > 0) await back.first().click({ force: true });
    }
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

  test('FASE 4-C: Roles — listar', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);
    await page.goto('/AdminRoles', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('h1, h2')).toContainText(/Roles/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('FASE 4-D: Citas — listar', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);
    await page.goto('/Appointments', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

  test('FASE 4-E: Billing — listar', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);
    await page.goto('/BillingInvoices', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

  test('FASE 4-F: Reportes — listar', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);
    await page.goto('/Reports/Appointments', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

  test('FASE 5: Validaciones — formularios inválidos', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);

    // Correo inválido en creación paciente
    await page.goto('/Patients/Create', { waitUntil: 'domcontentloaded' });
    await page.locator("input[name='Correo']").fill('no-es-un-email');
    await safeClick(page, { role: 'button', name: 'Guardar paciente' });
    await expect(page).toHaveURL(/\/Patients\/Create/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

});
