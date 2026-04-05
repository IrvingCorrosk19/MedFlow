import { test, expect } from '@playwright/test';
import { CREDS, loginStaff } from './_helpers';

// Verifica texto en el menú de navegación principal (nav-sidebar)
// Los links de reportes también dicen "Doctores", "Pacientes" etc., así que
// verificamos la sección "Clínica" / treeview por separado.
async function sidebarText(page: any): Promise<string> {
  return await page.locator('.main-sidebar nav').textContent() ?? '';
}

/** Enlace a Doctores bajo Clínica (/Doctors…), no confundir con Reportes (/Reports/Doctors). */
function clinicDoctorsLink(page: any) {
  return page.locator('.main-sidebar a[href^="/Doctors"]');
}

/** Ítems del menú Clínica (nav-link), no enlaces del dashboard (p. ej. small-box-footer). */
function sidebarClinicNavPatients(page: any) {
  return page.locator('.main-sidebar a.nav-link[href^="/Patients"]');
}

function sidebarClinicNavAppointments(page: any) {
  return page.locator('.main-sidebar a.nav-link[href^="/Appointments"]');
}

test.describe('Sidebar — visibilidad por rol', () => {

  test('SuperAdmin: SaaS, Clínica completa, Historia, Reportes, Billing, Seguridad, Admin', async ({ page }) => {
    await loginStaff(page, CREDS.superadmin.email, CREDS.superadmin.password);
    await page.goto('/Dashboard', { waitUntil: 'domcontentloaded' });
    const nav = page.locator('.main-sidebar nav');
    await expect(nav).toContainText('Plataforma SaaS');
    await expect(nav).toContainText('SuperAdmin');
    await expect(nav).toContainText('Operación clínica');
    await expect(nav).toContainText('Historia clínica');
    await expect(nav).toContainText('Inteligencia');
    await expect(nav).toContainText('Facturación y caja');
    await expect(nav).toContainText('Seguridad');
    await expect(nav).toContainText('Administración');
    // Clínica — item Doctores (puede estar colapsado; exige presencia en DOM, no visibilidad pintada)
    await expect(clinicDoctorsLink(page)).toHaveCount(1);
  });

  test('Admin: sin SaaS, Doctores en Clínica, Seguridad, Admin visibles', async ({ page }) => {
    await loginStaff(page, CREDS.admin.email, CREDS.admin.password);
    await page.goto('/Dashboard', { waitUntil: 'domcontentloaded' });
    const nav = page.locator('.main-sidebar nav');
    await expect(nav).not.toContainText('Plataforma SaaS');
    await expect(nav).toContainText('Operación clínica');
    await expect(nav).toContainText('Historia clínica');
    await expect(nav).toContainText('Seguridad');
    await expect(nav).toContainText('Administración');
    await expect(nav).toContainText('Facturación y caja');
    await expect(clinicDoctorsLink(page)).toHaveCount(1);
  });

  test('Doctor: sin SaaS, sin Doctores-link, sin Seguridad, sin Billing, sin Admin', async ({ page }) => {
    await loginStaff(page, CREDS.doctor.email, CREDS.doctor.password);
    await page.goto('/Dashboard', { waitUntil: 'domcontentloaded' });
    const nav = page.locator('.main-sidebar nav');
    await expect(nav).not.toContainText('Plataforma SaaS');
    await expect(nav).toContainText('Operación clínica');
    await expect(nav).toContainText('Historia clínica');
    await expect(nav).not.toContainText('Seguridad');
    await expect(nav).not.toContainText('Administración');
    await expect(nav).not.toContainText('Facturación y caja');
    // No item Clínica → Doctores (/Doctors); el dashboard puede enlazar a /Reports/Doctors
    await expect(clinicDoctorsLink(page)).toHaveCount(0);
  });

  test('Reception: Pacientes y Citas, sin Doctores, sin Historia, sin Seguridad, sin Billing', async ({ page }) => {
    await loginStaff(page, CREDS.reception.email, CREDS.reception.password);
    await page.goto('/Dashboard', { waitUntil: 'domcontentloaded' });
    const nav = page.locator('.main-sidebar nav');
    await expect(nav).not.toContainText('Plataforma SaaS');
    await expect(nav).toContainText('Operación clínica');
    await expect(nav).not.toContainText('Historia clínica');
    await expect(nav).not.toContainText('Seguridad');
    await expect(nav).not.toContainText('Administración');
    await expect(nav).not.toContainText('Facturación y caja');
    await expect(clinicDoctorsLink(page)).toHaveCount(0);
    await expect(sidebarClinicNavPatients(page)).toHaveCount(1);
    await expect(sidebarClinicNavAppointments(page)).toHaveCount(1);
  });

  test('Billing: Facturación y Reportes visibles, sin Clínica, sin Seguridad, sin Admin', async ({ page }) => {
    await loginStaff(page, CREDS.billing.email, CREDS.billing.password);
    await page.goto('/Dashboard', { waitUntil: 'domcontentloaded' });
    const nav = page.locator('.main-sidebar nav');
    await expect(nav).not.toContainText('Plataforma SaaS');
    await expect(nav).not.toContainText('Operación clínica');
    await expect(nav).not.toContainText('Historia clínica');
    await expect(nav).toContainText('Facturación y caja');
    await expect(nav).not.toContainText('Seguridad');
    await expect(nav).not.toContainText('Administración');
    await expect(clinicDoctorsLink(page)).toHaveCount(0);
    await expect(sidebarClinicNavPatients(page)).toHaveCount(0);
  });

  test('Staff: Pacientes y Citas, sin Doctores, sin Historia, sin Billing, sin Seguridad', async ({ page }) => {
    await loginStaff(page, CREDS.staff.email, CREDS.staff.password);
    await page.goto('/Dashboard', { waitUntil: 'domcontentloaded' });
    const nav = page.locator('.main-sidebar nav');
    await expect(nav).not.toContainText('Plataforma SaaS');
    await expect(nav).toContainText('Operación clínica');
    await expect(nav).not.toContainText('Historia clínica');
    await expect(nav).not.toContainText('Facturación y caja');
    await expect(nav).not.toContainText('Seguridad');
    await expect(nav).not.toContainText('Administración');
    await expect(clinicDoctorsLink(page)).toHaveCount(0);
    await expect(sidebarClinicNavPatients(page)).toHaveCount(1);
    await expect(sidebarClinicNavAppointments(page)).toHaveCount(1);
  });

});
