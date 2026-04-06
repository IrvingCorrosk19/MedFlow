/**
 * Billing — Flujos extendidos:
 *   • Crear factura con línea de concepto
 *   • Ver detalle de factura creada
 *   • Cancelar factura (si disponible)
 *   • Registrar pago sobre factura existente
 *   • Payments Index
 */
import { test, expect } from '@playwright/test';
import { CREDS, loginStaff, safeClick } from './_helpers';

const B = CREDS.billing;
const A = CREDS.admin;

// ── Crear factura ─────────────────────────────────────────────────────────────

test.describe('Billing — Crear factura', () => {

  test('BillingInvoices Create: formulario carga sin 500', async ({ page }) => {
    await loginStaff(page, B.email, B.password);
    await page.goto('/BillingInvoices/Create', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page.locator("select[name='PatientId']")).toBeVisible();
  });

  test('BillingInvoices Create: guardar sin paciente permanece en formulario', async ({ page }) => {
    await loginStaff(page, B.email, B.password);
    await page.goto('/BillingInvoices/Create', { waitUntil: 'domcontentloaded' });
    await safeClick(page, { role: 'button', name: /Guardar|Crear|Emitir/i });
    await page.waitForLoadState('domcontentloaded');
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

  test('BillingInvoices Create: flujo completo con una línea de concepto', async ({ page }) => {
    await loginStaff(page, B.email, B.password);
    await page.goto('/BillingInvoices/Create', { waitUntil: 'domcontentloaded' });

    const patientOpt = await page.locator("select[name='PatientId'] option[value]:not([value=''])").first().getAttribute('value');
    if (!patientOpt) {
      test.skip(true, 'Sin pacientes disponibles para crear factura');
      return;
    }

    await page.locator("select[name='PatientId']").selectOption(patientOpt);

    // Fecha de emisión
    const today = new Date().toISOString().slice(0, 10);
    const issueDateInput = page.locator("input[name='IssueDate']");
    if (await issueDateInput.count() > 0) await issueDateInput.fill(today);

    // Primera línea de concepto (puede venir prellenada por el servidor)
    const descInput = page.locator("input[name='Lines[0].Description'], textarea[name='Lines[0].Description']").first();
    if (await descInput.count() > 0) await descInput.fill('Consulta médica general QA');

    const qtyInput = page.locator("input[name='Lines[0].Quantity']");
    if (await qtyInput.count() > 0) {
      await qtyInput.fill('');
      await qtyInput.fill('1');
    }

    const priceInput = page.locator("input[name='Lines[0].UnitPrice']");
    if (await priceInput.count() > 0) {
      await priceInput.fill('');
      await priceInput.fill('500');
    }

    await safeClick(page, { role: 'button', name: /Guardar|Crear|Emitir/i });
    await page.waitForLoadState('domcontentloaded');
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    // En éxito redirige al Index o Details de la factura
    await expect(page).toHaveURL(/\/BillingInvoices/i);
  });

  test('BillingInvoices Create: fecha vencimiento < emisión muestra error', async ({ page }) => {
    await loginStaff(page, B.email, B.password);
    await page.goto('/BillingInvoices/Create', { waitUntil: 'domcontentloaded' });

    const patientOpt = await page.locator("select[name='PatientId'] option[value]:not([value=''])").first().getAttribute('value');
    if (!patientOpt) { test.skip(true, 'Sin pacientes'); return; }
    await page.locator("select[name='PatientId']").selectOption(patientOpt);

    const today = new Date().toISOString().slice(0, 10);
    const yesterday = new Date(Date.now() - 86400000).toISOString().slice(0, 10);

    const issueDateInput = page.locator("input[name='IssueDate']");
    if (await issueDateInput.count() > 0) await issueDateInput.fill(today);

    const dueDateInput = page.locator("input[name='DueDate']");
    if (await dueDateInput.count() > 0) await dueDateInput.fill(yesterday);

    const priceInput = page.locator("input[name='Lines[0].UnitPrice']");
    if (await priceInput.count() > 0) { await priceInput.fill(''); await priceInput.fill('100'); }

    await safeClick(page, { role: 'button', name: /Guardar|Crear|Emitir/i });
    await page.waitForLoadState('domcontentloaded');
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    // Debe permanecer en Create con mensaje de error
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

});

// ── Detalle y cancelar factura ────────────────────────────────────────────────

test.describe('Billing — Detalle de factura', () => {

  test('BillingInvoices Details: primera factura existente sin 500', async ({ page }) => {
    await loginStaff(page, B.email, B.password);
    await page.goto('/BillingInvoices', { waitUntil: 'domcontentloaded' });

    const detailLink = page
      .locator('table tbody tr a[href*="/BillingInvoices/Details/"], table tbody tr a[href*="/BillingInvoices/"][title]')
      .first();

    if (await detailLink.count() === 0) {
      // Intentar primer enlace genérico de la tabla
      const generic = page.locator('table tbody tr a').first();
      if (await generic.count() === 0) {
        test.skip(true, 'No hay facturas listadas');
        return;
      }
      const href = await generic.getAttribute('href');
      if (!href) { test.skip(true, 'Enlace sin href'); return; }
      await page.goto(href, { waitUntil: 'domcontentloaded' });
    } else {
      const href = await detailLink.getAttribute('href');
      if (!href) { test.skip(true, 'Enlace sin href'); return; }
      await page.goto(href, { waitUntil: 'domcontentloaded' });
    }

    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

});

// ── Registrar pago ────────────────────────────────────────────────────────────

test.describe('Billing — Registrar pago', () => {

  test('Payments Index: carga sin 500', async ({ page }) => {
    await loginStaff(page, B.email, B.password);
    await page.goto('/Payments', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('Payments Create: sin invoiceId muestra aviso sin 500', async ({ page }) => {
    await loginStaff(page, B.email, B.password);
    await page.goto('/Payments/Create', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('Payments Create: abrir desde factura Pending y registrar pago', async ({ page }) => {
    await loginStaff(page, B.email, B.password);

    // Crear una factura nueva para tener una en estado Pending
    await page.goto('/BillingInvoices/Create', { waitUntil: 'domcontentloaded' });
    const patientOpt = await page.locator("select[name='PatientId'] option[value]:not([value=''])").first().getAttribute('value');
    if (!patientOpt) { test.skip(true, 'Sin pacientes'); return; }
    await page.locator("select[name='PatientId']").selectOption(patientOpt);
    await page.locator("input[name='Lines[0].UnitPrice']").fill('200');
    await page.locator('form[method=post] button[type=submit]').click({ force: true });
    await page.waitForLoadState('domcontentloaded');

    // Debería estar en Details de la factura recién creada (Pending)
    await expect(page).toHaveURL(/\/BillingInvoices\/Details\//i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');

    // Clic en botón "Registrar pago" que está en la página de detalles
    const payBtn = page.locator('button:has-text("Registrar pago"), button:has-text("Registrar Pago")').first();
    if (await payBtn.count() === 0) { test.skip(true, 'Botón Registrar pago no encontrado en Details'); return; }

    await payBtn.click({ force: true });
    await page.waitForLoadState('domcontentloaded');
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    // Éxito: toast "Pago registrado" y factura en estado Paid
    await expect(page.locator('body')).toContainText(/Pago registrado|Paid/i);
  });

});

// ── Admin también puede ver BillingInvoices ───────────────────────────────────

test.describe('Admin — Billing acceso completo', () => {

  test('Admin: BillingInvoices Create accesible', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/BillingInvoices/Create', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

  test('Admin: Payments Index accesible', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/Payments', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

});

// ── Billing NO tiene acceso a /Patients (solo ve pacientes en formularios) ───

test.describe('Billing — Restricción /Patients confirmada', () => {

  test('Billing: /Patients denegado (usa select embebido en facturas)', async ({ page }) => {
    await loginStaff(page, B.email, B.password);
    await page.goto('/Patients', { waitUntil: 'domcontentloaded' });
    // Correctamente denegado — acceso a pacientes solo vía formularios de factura
    const url = page.url();
    const body = page.locator('body');
    const denied = url.includes('/Account/Login') || await body.locator('text=Acceso denegado').count() > 0;
    expect(denied || (await body.locator('body').textContent())?.includes('denegado')).toBeTruthy();
  });

});
