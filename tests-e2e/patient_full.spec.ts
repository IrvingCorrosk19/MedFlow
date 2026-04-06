import { test, expect } from '@playwright/test';
import { CREDS, loginPatient, expectAccessDenied } from './_helpers';

const P = CREDS.patient;

test.describe('Patient — Flujo completo', () => {

  test('FASE 2: Login Patient Portal', async ({ page }) => {
    await loginPatient(page, P.email, P.password);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('FASE 3: Navegación portal paciente', async ({ page }) => {
    await loginPatient(page, P.email, P.password);

    await page.goto('/PatientPortal/inicio', { waitUntil: 'domcontentloaded' });
    await expect(page).toHaveURL(/\/PatientPortal\/inicio/i);

    await page.goto('/PatientPortal/perfil', { waitUntil: 'domcontentloaded' });
    await expect(page).toHaveURL(/\/PatientPortal\/perfil/i);

    await page.goto('/PatientPortal/citas', { waitUntil: 'domcontentloaded' });
    await expect(page).toHaveURL(/\/PatientPortal\/citas/i);
  });

  test('FASE 4: Portal — inicio y citas sin errores 500', async ({ page }) => {
    await loginPatient(page, P.email, P.password);

    for (const route of ['/PatientPortal/inicio', '/PatientPortal/citas', '/PatientPortal/perfil']) {
      await page.goto(route, { waitUntil: 'domcontentloaded' });
      await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    }
  });

  test('FASE 5: Restricción — Dashboard staff denegado', async ({ page }) => {
    await loginPatient(page, P.email, P.password);
    await page.goto('/Dashboard', { waitUntil: 'domcontentloaded' });
    await expectAccessDenied(page);
  });

});
