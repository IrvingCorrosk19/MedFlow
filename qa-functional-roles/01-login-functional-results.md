# Fase 1 — Login funcional (QA)

**Base URL:** `http://localhost:5115`  
**Contraseña:** `Development:QaRoleUsersPassword` → `Medflow2026!Aa` (appsettings.Development.json)

| Usuario | Flujo | Resultado esperado | Resultado real | Notas |
|---------|--------|-------------------|----------------|-------|
| qa.admin@medflow.local | `POST /Account/Login` (form + antiforgery) | 200, cookie Identity, dashboard staff | OK | Sesión staff; redirección a `/` (Dashboard) |
| qa.reception@medflow.local | Igual | OK | OK | |
| qa.doctor@medflow.local | Igual | OK | OK | |
| qa.billing@medflow.local | Igual | OK | OK | |
| qa.staff@medflow.local | Igual | OK | OK | |
| qa.patient@medflow.local | **No** usar `/Account/Login` (rechaza Patient) | Portal paciente | OK | `POST /PatientPortal/login` → `/PatientPortal/inicio` |

**Navegador (muestra):** login staff → Dashboard ejecutivo; login portal → “Hola, QA Paciente” en `/PatientPortal/inicio`.

**Negativo:** Patient en `/Account/Login` muestra mensaje de usar portal (comportamiento definido en `AccountController`).
