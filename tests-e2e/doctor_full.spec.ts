import { test, expect } from '@playwright/test';
import { CREDS, loginStaff, expectAccessDenied } from './_helpers';

const D = CREDS.doctor;

test.describe('Doctor — Flujo completo', () => {

  test('FASE 2: Login Doctor', async ({ page }) => {
    await loginStaff(page, D.email, D.password);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('FASE 3: Navegación módulos permitidos', async ({ page }) => {
    await loginStaff(page, D.email, D.password);
    for (const mod of ['/Dashboard', '/Appointments', '/MedicalRecords', '/Patients']) {
      await page.goto(mod, { waitUntil: 'domcontentloaded' });
      await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
      await expect(page).not.toHaveURL(/\/Account\/Login/i);
    }
  });

  test('FASE 4-A: Citas — listar', async ({ page }) => {
    await loginStaff(page, D.email, D.password);
    await page.goto('/Appointments', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

  test('FASE 4-B: Expedientes médicos — listar', async ({ page }) => {
    await loginStaff(page, D.email, D.password);
    await page.goto('/MedicalRecords', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

  test('FASE 5: Restricciones — AdminUsers, BillingInvoices, AdminRoles denegados', async ({ page }) => {
    await loginStaff(page, D.email, D.password);
    for (const mod of ['/AdminUsers', '/BillingInvoices', '/AdminRoles']) {
      await page.goto(mod, { waitUntil: 'domcontentloaded' });
      await expectAccessDenied(page);
    }
  });

});
