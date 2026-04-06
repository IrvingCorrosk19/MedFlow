import { test, expect } from '@playwright/test';
import { CREDS, loginStaff, expectAccessDenied } from './_helpers';

const S = CREDS.staff;

test.describe('Staff — Flujo completo', () => {

  test('FASE 2: Login Staff', async ({ page }) => {
    await loginStaff(page, S.email, S.password);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('FASE 3: Navegación módulos permitidos', async ({ page }) => {
    await loginStaff(page, S.email, S.password);
    for (const mod of ['/Dashboard', '/Patients', '/Appointments']) {
      await page.goto(mod, { waitUntil: 'domcontentloaded' });
      await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
      await expect(page).not.toHaveURL(/\/Account\/Login/i);
    }
  });

  test('FASE 4-A: Pacientes — listar y ver detalle', async ({ page }) => {
    await loginStaff(page, S.email, S.password);
    await page.goto('/Patients', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');

    // Ver primera fila si existe
    const firstRow = page.locator('#tblPacientes tbody tr').first();
    if (await firstRow.count() > 0) {
      const detailLink = firstRow.locator('a').first();
      if (await detailLink.count() > 0) {
        await detailLink.click({ force: true });
        await page.waitForLoadState('domcontentloaded');
        await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
        await page.goBack();
      }
    }
  });

  test('FASE 4-B: Citas — listar', async ({ page }) => {
    await loginStaff(page, S.email, S.password);
    await page.goto('/Appointments', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

  test('FASE 5: Restricciones — AdminUsers, AdminRoles, BillingInvoices, MedicalRecords denegados', async ({ page }) => {
    await loginStaff(page, S.email, S.password);
    for (const mod of ['/AdminUsers', '/AdminRoles', '/BillingInvoices', '/MedicalRecords']) {
      await page.goto(mod, { waitUntil: 'domcontentloaded' });
      await expectAccessDenied(page);
    }
  });

});
