import { test, expect } from '@playwright/test';
import { CREDS, loginStaff, safeClick, unique, expectAccessDenied } from './_helpers';

const R = CREDS.reception;

test.describe('Reception — Flujo completo', () => {

  test('FASE 2: Login Reception', async ({ page }) => {
    await loginStaff(page, R.email, R.password);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('FASE 3: Navegación módulos permitidos', async ({ page }) => {
    await loginStaff(page, R.email, R.password);
    for (const mod of ['/Dashboard', '/Patients', '/Appointments']) {
      await page.goto(mod, { waitUntil: 'domcontentloaded' });
      await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
      await expect(page).not.toHaveURL(/\/Account\/Login/i);
    }
  });

  test('FASE 4-A: Pacientes — CRUD completo', async ({ page }) => {
    await loginStaff(page, R.email, R.password);

    await page.goto('/Patients', { waitUntil: 'domcontentloaded' });
    await safeClick(page, { role: 'link', name: 'Nuevo paciente' });
    await expect(page).toHaveURL(/\/Patients\/Create/i);

    // NEGATIVO: guardar vacío
    await safeClick(page, { role: 'button', name: 'Guardar paciente' });
    await expect(page).toHaveURL(/\/Patients\/Create/i);

    const lastName = unique('RC-Ap');
    const doc = unique('RC').replace(/[^0-9A-Za-z]/g, '').slice(0, 10);
    await page.locator("input[name='PrimerNombre']").fill('María');
    await page.locator("input[name='PrimerApellido']").fill(lastName);
    await page.locator("input[name='FechaNacimiento']").fill('1999-02-20');
    const sexo = await page.locator("select[name='Sexo'] option[value]:not([value=''])").first().getAttribute('value');
    const tipo = await page.locator("select[name='TipoDocumento'] option[value]:not([value=''])").first().getAttribute('value');
    if (sexo) await page.locator("select[name='Sexo']").selectOption(sexo);
    if (tipo) await page.locator("select[name='TipoDocumento']").selectOption(tipo);
    await page.locator("input[name='NumeroDocumento']").fill(doc);
    await page.locator("input[name='Correo']").fill(`qa.rc.${Date.now()}@medflow.local`);
    await safeClick(page, { role: 'button', name: 'Guardar paciente' });
    await expect(page).toHaveURL(/\/Patients/i);

    // EDITAR
    const row = page.locator(`#tblPacientes tbody tr:has-text("${lastName}")`).first();
    await expect(row).toBeVisible({ timeout: 10000 });
    await row.locator("a[title='Editar']").click({ force: true });
    await expect(page).toHaveURL(/\/Patients\/Edit\//i);
    await page.locator("input[name='Telefono']").fill('555-0002');
    await safeClick(page, { role: 'button', name: 'Guardar cambios' });
    await expect(page).toHaveURL(/\/Patients/i);
  });

  test('FASE 4-B: Citas — listar', async ({ page }) => {
    await loginStaff(page, R.email, R.password);
    await page.goto('/Appointments', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('h1, h2')).toContainText(/Citas/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('FASE 5: Restricciones — AdminUsers, MedicalRecords, BillingInvoices denegados', async ({ page }) => {
    await loginStaff(page, R.email, R.password);
    for (const mod of ['/AdminUsers', '/MedicalRecords', '/BillingInvoices']) {
      await page.goto(mod, { waitUntil: 'domcontentloaded' });
      await expectAccessDenied(page);
    }
  });

});
