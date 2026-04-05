import { test, expect } from '@playwright/test';
import { CREDS, loginStaff, safeClick, unique, expectAccessDenied } from './_helpers';

test('Reception: login + pacientes + citas + restricciones', async ({ page }) => {
  await loginStaff(page, CREDS.reception.email, CREDS.reception.password);

  // Pacientes: crear + buscar + editar
  await page.goto('/Patients', { waitUntil: 'domcontentloaded' });
  await safeClick(page, { role: 'link', name: 'Nuevo paciente' });
  await expect(page).toHaveURL(/\/Patients\/Create/i);

  const lastName = unique('UAT-Recep-Ap');
  const doc = unique('RC').replace(/[^0-9A-Za-z]/g, '').slice(0, 10);
  await page.locator("input[name='PrimerNombre']").fill('María');
  await page.locator("input[name='PrimerApellido']").fill(lastName);
  await page.locator("input[name='FechaNacimiento']").fill('1999-02-20');
  const sexo = await page.locator("select[name='Sexo'] option[value]:not([value=''])").first().getAttribute('value');
  const tipo = await page.locator("select[name='TipoDocumento'] option[value]:not([value=''])").first().getAttribute('value');
  if (sexo) await page.locator("select[name='Sexo']").selectOption(sexo);
  if (tipo) await page.locator("select[name='TipoDocumento']").selectOption(tipo);
  await page.locator("input[name='NumeroDocumento']").fill(doc);
  await page.locator("input[name='Telefono']").fill('555-0001');
  await page.locator("input[name='Correo']").fill(`qa.recep.${Date.now()}@medflow.local`);
  await safeClick(page, { role: 'button', name: 'Guardar paciente' });
  await expect(page).toHaveURL(/\/Patients/i);

  // Buscar paciente
  await page.locator("input[name='search']").fill(lastName);
  await safeClick(page, { role: 'button', name: 'Aplicar' });
  await expect(page.locator(`#tblPacientes tbody tr:has-text("${lastName}")`).first()).toBeVisible();

  // Editar paciente
  await page.locator(`#tblPacientes tbody tr:has-text("${lastName}")`).first().locator("a[title='Editar']").click({ force: true });
  await expect(page).toHaveURL(/\/Patients\/Edit\//i);
  await page.locator("input[name='Telefono']").fill('555-0002');
  await safeClick(page, { role: 'button', name: 'Guardar cambios' });
  await expect(page).toHaveURL(/\/Patients/i);

  // Citas: abrir módulo (crear completo depende de UI, validar carga + negativo mínimo)
  await page.goto('/Appointments', { waitUntil: 'domcontentloaded' });
  await expect(page.locator('h1, h2')).toContainText(/Citas/i);

  // Restricciones
  await page.goto('/AdminUsers', { waitUntil: 'domcontentloaded' });
  await expectAccessDenied(page);
  await page.goto('/MedicalRecords', { waitUntil: 'domcontentloaded' });
  await expectAccessDenied(page);
  await page.goto('/BillingInvoices', { waitUntil: 'domcontentloaded' });
  await expectAccessDenied(page);
});

