import { expect, type Page } from '@playwright/test';

const PWD = 'Medflow2026!Aa';

export const CREDS = {
  superadmin: { email: 'superadmin@medflow.ai', password: PWD },
  admin: { email: 'admin@medflow.ai', password: PWD },
  // QA users — password garantizada por QaRoleUsersPassword seed
  reception: { email: 'qa.reception@medflow.local', password: PWD },
  doctor: { email: 'qa.doctor@medflow.local', password: PWD },
  billing: { email: 'qa.billing@medflow.local', password: PWD },
  staff: { email: 'qa.staff@medflow.local', password: PWD },
  patient: { email: 'qa.patient@medflow.local', password: PWD },
  qaAdmin: { email: 'qa.admin@medflow.local', password: PWD },
};

export async function loginStaff(page: Page, email: string, password: string) {
  await page.goto('/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.locator("input[name='Email']").fill(email);
  await page.locator("input[name='Password']").fill(password);
  await page.getByRole('button', { name: 'Entrar' }).click({ force: true });
  await page.waitForLoadState('domcontentloaded');
  await expect(page).not.toHaveURL(/\/Account\/Login/i);
}

export async function loginPatient(page: Page, email: string, password: string) {
  await page.goto('/PatientPortal/login', { waitUntil: 'domcontentloaded' });
  await page.locator("input[name='Email']").fill(email);
  await page.locator("input[name='Password']").fill(password);
  await page.getByRole('button', { name: 'Iniciar sesión' }).click({ force: true });
  await page.waitForLoadState('domcontentloaded');
  await expect(page).toHaveURL(/\/PatientPortal\//i);
}

export async function expectAccessDenied(page: Page) {
  await page.waitForLoadState('domcontentloaded');
  await expect(page.locator("text=Acceso denegado").first()).toBeVisible();
}

export async function safeClick(page: Page, selectorOrRole: { role?: 'button' | 'link'; name?: string; selector?: string }) {
  if (selectorOrRole.selector) {
    const loc = page.locator(selectorOrRole.selector);
    await loc.scrollIntoViewIfNeeded();
    await loc.click({ force: true });
    return;
  }
  if (selectorOrRole.role && selectorOrRole.name) {
    const loc =
      selectorOrRole.role === 'button'
        ? page.getByRole('button', { name: selectorOrRole.name })
        : page.getByRole('link', { name: selectorOrRole.name });
    await loc.scrollIntoViewIfNeeded();
    await loc.click({ force: true });
    return;
  }
  throw new Error('safeClick: missing selector or role+name');
}

export function unique(prefix: string) {
  return `${prefix}-${Date.now()}-${Math.random().toString(16).slice(2, 8)}`;
}

