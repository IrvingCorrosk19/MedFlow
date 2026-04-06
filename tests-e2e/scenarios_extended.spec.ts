import { test, expect } from '@playwright/test';
import { CREDS, loginStaff, loginPatient, expectAccessDenied, safeClick, unique } from './_helpers';

test.describe('Escenarios extendidos — auth y límites por rol', () => {
  test('Staff: contraseña incorrecta permanece en login y muestra mensaje', async ({ page }) => {
    await page.goto('/Account/Login', { waitUntil: 'domcontentloaded' });
    await page.locator("input[name='Email']").fill(CREDS.admin.email);
    await page.locator("input[name='Password']").fill('___clave_incorrecta___');
    await page.getByRole('button', { name: 'Entrar' }).click({ force: true });
    await page.waitForLoadState('domcontentloaded');
    await expect(page).toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('.mf-validation-summary').first()).toContainText(/incorrectos/i);
  });

  test('Anónimo: acceso a /Patients redirige a login', async ({ page }) => {
    await page.goto('/Patients', { waitUntil: 'domcontentloaded' });
    await expect(page).toHaveURL(/\/Account\/Login/i);
  });

  test('Admin: logout y vuelta a entrar con credenciales válidas', async ({ page }) => {
    await loginStaff(page, CREDS.admin.email, CREDS.admin.password);
    await page.goto('/Dashboard', { waitUntil: 'domcontentloaded' });

    const userMenu = page.locator('.main-header .nav-item.dropdown > a.nav-link').filter({ hasText: '@' });
    await userMenu.click({ force: true });
    await page.locator('.main-header .dropdown-menu button.dropdown-item, .main-header form[action*="Logout"] button').first().click({ force: true });
    await expect(page).toHaveURL(/\/Account\/Login/i);

    await loginStaff(page, CREDS.admin.email, CREDS.admin.password);
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

  test('Recepción: /Doctors (gestión doctores) denegado', async ({ page }) => {
    await loginStaff(page, CREDS.reception.email, CREDS.reception.password);
    await page.goto('/Doctors', { waitUntil: 'domcontentloaded' });
    await expectAccessDenied(page);
  });

  test('Facturación: /Appointments denegado (sin rol de citas)', async ({ page }) => {
    await loginStaff(page, CREDS.billing.email, CREDS.billing.password);
    await page.goto('/Appointments', { waitUntil: 'domcontentloaded' });
    await expectAccessDenied(page);
  });

  test('Staff: /MedicalRecords (historia) denegado', async ({ page }) => {
    await loginStaff(page, CREDS.staff.email, CREDS.staff.password);
    await page.goto('/MedicalRecords', { waitUntil: 'domcontentloaded' });
    await expectAccessDenied(page);
  });

  test('Admin: área SuperAdmin denegada', async ({ page }) => {
    await loginStaff(page, CREDS.admin.email, CREDS.admin.password);
    await page.goto('/SuperAdmin', { waitUntil: 'domcontentloaded' });
    await expectAccessDenied(page);
  });

  test('Paciente portal: contraseña incorrecta no entra', async ({ page }) => {
    await page.goto('/PatientPortal/login', { waitUntil: 'domcontentloaded' });
    await page.locator("input[name='Email']").fill(CREDS.patient.email);
    await page.locator("input[name='Password']").fill('___mala___');
    await page.getByRole('button', { name: 'Iniciar sesión' }).click({ force: true });
    await page.waitForLoadState('domcontentloaded');
    await expect(page).toHaveURL(/\/PatientPortal\/login/i);
    await expect(page.locator('body')).toContainText(/incorrectas|inválid/i);
  });

  test('Usuario staff en login del portal paciente: mensaje de área incorrecta', async ({ page }) => {
    await page.goto('/PatientPortal/login', { waitUntil: 'domcontentloaded' });
    await page.locator("input[name='Email']").fill(CREDS.reception.email);
    await page.locator("input[name='Password']").fill(CREDS.reception.password);
    await page.getByRole('button', { name: 'Iniciar sesión' }).click({ force: true });
    await page.waitForLoadState('domcontentloaded');
    await expect(page).toHaveURL(/\/PatientPortal\/login/i);
    await expect(page.locator('body')).toContainText(/solo para pacientes|personal/i);
  });
});

test.describe('Escenarios extendidos — flujos de datos', () => {
  test('Doctor: búsqueda de pacientes sin 500 (listado reactivo o vacío)', async ({ page }) => {
    await loginStaff(page, CREDS.doctor.email, CREDS.doctor.password);
    await page.goto('/Patients', { waitUntil: 'domcontentloaded' });
    const q = unique('BusqDoc');
    await page.locator("input[name='search']").fill(q);
    await safeClick(page, { role: 'button', name: 'Aplicar' });
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

  test('Recepción: detalle de paciente desde fila (ver) sin error', async ({ page }) => {
    await loginStaff(page, CREDS.reception.email, CREDS.reception.password);
    await page.goto('/Patients', { waitUntil: 'domcontentloaded' });
    const firstDetail = page.locator('#tblPacientes tbody tr').first().locator("a[title='Ver']").first();
    if ((await firstDetail.count()) === 0) {
      test.skip(true, 'No hay filas de pacientes para abrir detalle');
      return;
    }
    const href = await firstDetail.getAttribute('href');
    if (!href) {
      test.skip(true, 'Enlace Ver sin href');
      return;
    }
    // DataTables a veces deja el botón fuera del viewport “visible” para Playwright; navegar por URL es estable.
    await page.goto(href, { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('domcontentloaded');
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

  test('Paciente: tras login, perfil carga sin 500', async ({ page }) => {
    await loginPatient(page, CREDS.patient.email, CREDS.patient.password);
    await page.goto('/PatientPortal/perfil', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });
});
