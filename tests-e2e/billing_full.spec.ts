import { test, expect } from '@playwright/test';
import { CREDS, loginStaff, safeClick, expectAccessDenied } from './_helpers';

const B = CREDS.billing;

test.describe('Billing — Flujo completo', () => {

  test('FASE 2: Login Billing', async ({ page }) => {
    await loginStaff(page, B.email, B.password);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('FASE 3: Navegación módulos permitidos', async ({ page }) => {
    await loginStaff(page, B.email, B.password);
    // Billing no tiene PatientsView; sólo puede ver pacientes embebidos en formularios de factura
    for (const mod of ['/Dashboard', '/BillingInvoices', '/Payments']) {
      await page.goto(mod, { waitUntil: 'domcontentloaded' });
      await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
      await expect(page).not.toHaveURL(/\/Account\/Login/i);
    }
  });

  test('FASE 4-A: Facturas — listar y acceder', async ({ page }) => {
    await loginStaff(page, B.email, B.password);
    await page.goto('/BillingInvoices', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);

    // Intentar crear factura
    const newLink = page.getByRole('link', { name: /Nueva factura|Crear factura|Nueva/i }).first();
    if (await newLink.count() > 0) {
      await newLink.click({ force: true });
      await page.waitForLoadState('domcontentloaded');
      await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
      const back = page.getByRole('link', { name: /Volver|Cancelar/i });
      if (await back.count() > 0) await back.first().click({ force: true });
    }
  });

  test('FASE 4-B: Pagos — listar', async ({ page }) => {
    await loginStaff(page, B.email, B.password);
    await page.goto('/Payments', { waitUntil: 'domcontentloaded' });
    const status = page.url();
    // Si existe la ruta debe cargar sin 500
    if (!status.includes('/Account/Login')) {
      await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    }
  });

  test('FASE 5: Restricciones — AdminUsers, AdminRoles, MedicalRecords denegados', async ({ page }) => {
    await loginStaff(page, B.email, B.password);
    for (const mod of ['/AdminUsers', '/AdminRoles', '/MedicalRecords']) {
      await page.goto(mod, { waitUntil: 'domcontentloaded' });
      await expectAccessDenied(page);
    }
  });

});
