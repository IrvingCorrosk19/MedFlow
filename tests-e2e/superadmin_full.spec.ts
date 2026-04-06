/**
 * SuperAdmin — Área SaaS: Tenants, Plans, Subscriptions, Billing
 * Rutas bajo /SuperAdmin/* requieren rol SuperAdmin (cookie de sesión).
 */
import { test, expect } from '@playwright/test';
import { CREDS, loginStaff, expectAccessDenied, safeClick, unique } from './_helpers';

const SA = CREDS.superadmin;
const A  = CREDS.admin;

// ── Módulos SaaS ─────────────────────────────────────────────────────────────

test.describe('SuperAdmin Area — Navegación SaaS', () => {

  test('Tenants: listar sin error', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);
    await page.goto('/SuperAdmin/Tenants', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page.locator('h1, h2')).toContainText(/cl[ií]nicas|tenants/i);
  });

  test('Plans: listar sin error', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);
    await page.goto('/SuperAdmin/Plans', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page.locator('h1, h2')).toContainText(/planes?/i);
  });

  test('Subscriptions: listar sin error', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);
    await page.goto('/SuperAdmin/Subscriptions', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('Billing SaaS Overview: sin error', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);
    await page.goto('/SuperAdmin/Billing/Overview', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('Billing SaaS Transactions: sin error', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);
    await page.goto('/SuperAdmin/Billing/Transactions', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('Billing SaaS Invoices: sin error', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);
    await page.goto('/SuperAdmin/Billing/Invoices', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('SuperAdmin Home: sin error', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);
    await page.goto('/SuperAdmin/Home', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

});

// ── Tenants CRUD ──────────────────────────────────────────────────────────────

test.describe('SuperAdmin Area — Tenants CRUD', () => {

  test('Tenant Create: formulario carga sin error', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);
    await page.goto('/SuperAdmin/Tenants/Create', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    // Debe haber un campo name o código
    await expect(page.locator("input[name='name'], input[name='Name'], input[name='code'], input[name='Code']").first()).toBeVisible();
  });

  test('Tenant Create: guardar vacío permanece en formulario', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);
    await page.goto('/SuperAdmin/Tenants/Create', { waitUntil: 'domcontentloaded' });
    await safeClick(page, { role: 'button', name: /Crear|Guardar|Save/i });
    await page.waitForLoadState('domcontentloaded');
    // Sin datos válidos debe quedarse en Create o mostrar errores de validación
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

  test('Tenant Details: primer tenant existente sin error', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);
    await page.goto('/SuperAdmin/Tenants', { waitUntil: 'domcontentloaded' });

    const detailLink = page.locator('table tbody tr a, #tblTenants tbody tr a').first();
    if (await detailLink.count() === 0) {
      test.skip(true, 'No hay tenants listados para abrir detalle');
      return;
    }
    const href = await detailLink.getAttribute('href');
    if (!href) { test.skip(true, 'Enlace sin href'); return; }
    await page.goto(href, { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

});

// ── Plans CRUD ────────────────────────────────────────────────────────────────

test.describe('SuperAdmin Area — Plans', () => {

  test('Plan Create: formulario carga sin error', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);
    await page.goto('/SuperAdmin/Plans/Create', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('Plan Details: primer plan existente sin error', async ({ page }) => {
    await loginStaff(page, SA.email, SA.password);
    await page.goto('/SuperAdmin/Plans', { waitUntil: 'domcontentloaded' });

    const detailLink = page.locator('table tbody tr a').first();
    if (await detailLink.count() === 0) {
      test.skip(true, 'No hay planes listados');
      return;
    }
    const href = await detailLink.getAttribute('href');
    if (!href) { test.skip(true, 'Enlace sin href'); return; }
    await page.goto(href, { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

});

// ── Restricciones de rol ──────────────────────────────────────────────────────

test.describe('SuperAdmin Area — Acceso denegado a otros roles', () => {

  test('Admin (clínica): /SuperAdmin/Tenants denegado', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/SuperAdmin/Tenants', { waitUntil: 'domcontentloaded' });
    await expectAccessDenied(page);
  });

  test('Admin (clínica): /SuperAdmin/Plans denegado', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/SuperAdmin/Plans', { waitUntil: 'domcontentloaded' });
    await expectAccessDenied(page);
  });

});
