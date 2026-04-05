import { test, expect } from '@playwright/test';
import { CREDS, loginStaff, safeClick, unique } from './_helpers';

test('Admin: login + navegación + acciones básicas', async ({ page }) => {
  await loginStaff(page, CREDS.admin.email, CREDS.admin.password);

  // Dashboard
  await page.goto('/Dashboard', { waitUntil: 'domcontentloaded' });
  await expect(page.locator('h1')).toContainText(/Dashboard|Inicio/i);

  // Pacientes: crear, validar requerido, editar, cancelar
  await page.goto('/Patients', { waitUntil: 'domcontentloaded' });
  await safeClick(page, { role: 'link', name: 'Nuevo paciente' });
  await expect(page).toHaveURL(/\/Patients\/Create/i);

  // NEGATIVO: intentar guardar incompleto
  await safeClick(page, { role: 'button', name: 'Guardar paciente' });
  await expect(page).toHaveURL(/\/Patients\/Create/i);

  const lastName = unique('UAT-Admin-Ap');
  const doc = unique('AD').replace(/[^0-9A-Za-z]/g, '').slice(0, 10);
  await page.locator("input[name='PrimerNombre']").fill('QA');
  await page.locator("input[name='PrimerApellido']").fill(lastName);
  await page.locator("input[name='FechaNacimiento']").fill('1995-01-10');

  // selects (primera opción no vacía)
  const sexo = await page.locator("select[name='Sexo'] option[value]:not([value=''])").first().getAttribute('value');
  const tipo = await page.locator("select[name='TipoDocumento'] option[value]:not([value=''])").first().getAttribute('value');
  if (sexo) await page.locator("select[name='Sexo']").selectOption(sexo);
  if (tipo) await page.locator("select[name='TipoDocumento']").selectOption(tipo);
  await page.locator("input[name='NumeroDocumento']").fill(doc);
  await page.locator("input[name='Correo']").fill(`qa.admin.${Date.now()}@medflow.local`);

  await safeClick(page, { role: 'button', name: 'Guardar paciente' });
  await expect(page).toHaveURL(/\/Patients/i);

  // EDITAR: buscar por apellido y entrar a editar
  const row = page.locator(`#tblPacientes tbody tr:has-text("${lastName}")`).first();
  await expect(row).toBeVisible();
  await row.locator("a[title='Editar']").click({ force: true });
  await expect(page).toHaveURL(/\/Patients\/Edit\//i);

  // CANCELAR sin guardar
  await page.locator("input[name='Telefono']").fill('999-NO-GUARDAR');
  await safeClick(page, { role: 'link', name: 'Volver al listado' });
  await expect(page).toHaveURL(/\/Patients/i);

  // Guardar cambio real
  const row2 = page.locator(`#tblPacientes tbody tr:has-text("${lastName}")`).first();
  await row2.locator("a[title='Editar']").click({ force: true });
  await page.locator("input[name='Telefono']").fill('555-8888');
  await safeClick(page, { role: 'button', name: 'Guardar cambios' });
  await expect(page).toHaveURL(/\/Patients/i);

  // Admin: seguridad (usuarios/roles) carga
  await page.goto('/AdminUsers', { waitUntil: 'domcontentloaded' });
  await expect(page.locator('h1, h2')).toContainText(/Usuarios/i);
  await page.goto('/AdminRoles', { waitUntil: 'domcontentloaded' });
  await expect(page.locator('h1, h2')).toContainText(/Roles/i);
});

