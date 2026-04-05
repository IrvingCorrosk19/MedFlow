import { test, expect } from '@playwright/test';
import { CREDS, loginPatient, expectAccessDenied } from './_helpers';

test('Patient: login + portal (inicio/perfil/citas) + restricciones', async ({ page }) => {
  await loginPatient(page, CREDS.patient.email, CREDS.patient.password);

  await page.goto('/PatientPortal/inicio', { waitUntil: 'domcontentloaded' });
  await expect(page).toHaveURL(/\/PatientPortal\/inicio/i);

  await page.goto('/PatientPortal/perfil', { waitUntil: 'domcontentloaded' });
  await expect(page).toHaveURL(/\/PatientPortal\/perfil/i);

  await page.goto('/PatientPortal/citas', { waitUntil: 'domcontentloaded' });
  await expect(page).toHaveURL(/\/PatientPortal\/citas/i);

  // Restricción: portal staff
  await page.goto('/Dashboard', { waitUntil: 'domcontentloaded' });
  await expectAccessDenied(page);
});

