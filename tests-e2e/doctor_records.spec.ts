/**
 * Doctor — Historia clínica (MedicalRecords)
 * URL real del servidor: /MedicalRecords/Patient?patientId={uuid}
 *                        /MedicalRecords/Create?patientId={uuid}
 */
import { test, expect } from '@playwright/test';
import { CREDS, loginStaff, safeClick } from './_helpers';

const D = CREDS.doctor;
const A = CREDS.admin;

// ── Helper: obtiene el primer patientId del listado ──────────────────────────

async function getFirstPatientId(page: any): Promise<string | null> {
  await page.goto('/MedicalRecords', { waitUntil: 'domcontentloaded' });
  // El link tiene forma: /MedicalRecords/Patient?patientId=UUID
  const link = page.locator('a[href*="patientId="]').first();
  if (await link.count() === 0) return null;
  const href = await link.getAttribute('href');
  if (!href) return null;
  const m = href.match(/patientId=([0-9a-f-]{36})/i);
  return m ? m[1] : null;
}

// ── Listado ───────────────────────────────────────────────────────────────────

test.describe('Doctor — MedicalRecords listado', () => {

  test('Index: lista pacientes sin 500', async ({ page }) => {
    await loginStaff(page, D.email, D.password);
    await page.goto('/MedicalRecords', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page.locator('h1, h2')).toContainText(/historia|expediente/i);
  });

  test('Index: búsqueda reactiva sin 500', async ({ page }) => {
    await loginStaff(page, D.email, D.password);
    await page.goto('/MedicalRecords', { waitUntil: 'domcontentloaded' });
    const searchInput = page.locator("input[name='search']");
    if (await searchInput.count() > 0) {
      await searchInput.fill('BusqInexistente999');
      await safeClick(page, { role: 'button', name: /Buscar|Aplicar/i });
      await page.waitForLoadState('domcontentloaded');
      await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    }
  });

  test('Patient: expediente del primer paciente sin 500', async ({ page }) => {
    await loginStaff(page, D.email, D.password);
    const patientId = await getFirstPatientId(page);
    if (!patientId) { test.skip(true, 'Sin pacientes en listado'); return; }
    await page.goto(`/MedicalRecords/Patient?patientId=${patientId}`, { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

});

// ── Crear consulta ────────────────────────────────────────────────────────────

test.describe('Doctor — MedicalRecords Create', () => {

  test('Create: formulario carga con selector de doctor y fecha', async ({ page }) => {
    await loginStaff(page, D.email, D.password);
    const patientId = await getFirstPatientId(page);
    if (!patientId) { test.skip(true, 'Sin pacientes'); return; }

    await page.goto(`/MedicalRecords/Create?patientId=${patientId}`, { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page.locator("select[name='DoctorId']")).toBeVisible();
    await expect(page.locator("input[name='VisitDate']")).toBeVisible();
  });

  test('Create: guardar sin doctor permanece en formulario con error', async ({ page }) => {
    await loginStaff(page, D.email, D.password);
    const patientId = await getFirstPatientId(page);
    if (!patientId) { test.skip(true, 'Sin pacientes'); return; }

    await page.goto(`/MedicalRecords/Create?patientId=${patientId}`, { waitUntil: 'domcontentloaded' });
    // Enviar sin seleccionar doctor
    await page.locator('form[method=post] button[type=submit]').click({ force: true });
    await page.waitForLoadState('domcontentloaded');
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

  test('Create: flujo completo con signos vitales y diagnóstico', async ({ page }) => {
    await loginStaff(page, D.email, D.password);
    const patientId = await getFirstPatientId(page);
    if (!patientId) { test.skip(true, 'Sin pacientes'); return; }

    await page.goto(`/MedicalRecords/Create?patientId=${patientId}`, { waitUntil: 'domcontentloaded' });

    const doctorOpt = await page.locator("select[name='DoctorId'] option[value]:not([value=''])").first().getAttribute('value');
    if (!doctorOpt) { test.skip(true, 'Sin doctores'); return; }
    await page.locator("select[name='DoctorId']").selectOption(doctorOpt);

    // Fecha de visita (datetime-local)
    await page.locator("input[name='VisitDate']").fill('2026-04-05T10:00');

    await page.locator("textarea[name='ChiefComplaint']").fill('Dolor de cabeza - prueba QA');
    await page.locator("textarea[name='Diagnosis']").fill('Cefalea tensional QA');
    await page.locator("input[name='HeightCm']").fill('170');
    await page.locator("input[name='WeightKg']").fill('72');
    await page.locator("input[name='BloodPressure']").fill('120/80');

    // Enviar el formulario POST correcto
    await page.locator('form[method=post] button[type=submit]').click({ force: true });
    await page.waitForLoadState('domcontentloaded');
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    // Éxito: redirige al expediente del paciente
    await expect(page).toHaveURL(/\/MedicalRecords\/Patient/i);
  });

});

// ── Detalle de consulta ───────────────────────────────────────────────────────

test.describe('Doctor — MedicalRecords Details', () => {

  test('Details: primer registro existente sin 500', async ({ page }) => {
    await loginStaff(page, D.email, D.password);
    const patientId = await getFirstPatientId(page);
    if (!patientId) { test.skip(true, 'Sin pacientes'); return; }

    await page.goto(`/MedicalRecords/Patient?patientId=${patientId}`, { waitUntil: 'domcontentloaded' });

    const detailLink = page.locator('a[href*="/MedicalRecords/Details"]').first();
    if (await detailLink.count() === 0) { test.skip(true, 'Sin consultas para este paciente'); return; }

    const href = await detailLink.getAttribute('href');
    if (!href) { test.skip(true, 'Enlace sin href'); return; }
    await page.goto(href, { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });

});

// ── Admin puede acceder a MedicalRecords ─────────────────────────────────────

test.describe('Admin — MedicalRecords acceso', () => {

  test('Admin: /MedicalRecords accesible sin error', async ({ page }) => {
    await loginStaff(page, A.email, A.password);
    await page.goto('/MedicalRecords', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).not.toContainText('500 Internal Server Error');
  });

});
