/**
 * Verificación rápida: cada par correo/contraseña de QA puede iniciar sesión.
 * Contraseña debe coincidir con Development:QaRoleUsersPassword / Apply* en appsettings.Development.json
 * y con tests-e2e/_helpers.ts (PWD).
 */
import { test, expect } from '@playwright/test';
import { CREDS, loginStaff, loginPatient } from './_helpers';

const staffCases: { key: string; email: string; password: string }[] = [
  { key: 'SuperAdmin', ...CREDS.superadmin },
  { key: 'Admin (clínica)', ...CREDS.admin },
  { key: 'Admin QA', ...CREDS.qaAdmin },
  { key: 'Reception', ...CREDS.reception },
  { key: 'Doctor', ...CREDS.doctor },
  { key: 'Billing', ...CREDS.billing },
  { key: 'Staff', ...CREDS.staff },
];

for (const { key, email, password } of staffCases) {
  test(`Login staff OK: ${key} (${email})`, async ({ page }) => {
    await loginStaff(page, email, password);
    await expect(page).not.toHaveURL(/\/Account\/Login/i);
  });
}

test('Login portal paciente OK (qa.patient@medflow.local)', async ({ page }) => {
  await loginPatient(page, CREDS.patient.email, CREDS.patient.password);
  await expect(page).toHaveURL(/\/PatientPortal\//i);
});
