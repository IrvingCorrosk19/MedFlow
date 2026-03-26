# Vulnerabilidades / brechas cerradas en esta pasada

1. **No había login JWT para roles de clínica** — solo paciente móvil; personal no podía validar tokens con roles. **Corrección:** `TenantStaffAuthService` + `POST /api/v1/auth/staff/login`.
2. **QA paciente sin registro `Patients`** — portal/API paciente imposible. **Corrección:** sembrado `SeedQaPatientPortalPatientAsync`.
3. **Suscripción PastDue bloqueaba toda la UI** — imposible probar permisos por rol en MVC. **Corrección:** `Saas:AllowOperationsWhenPastDue: true` en `appsettings.Development.json` (solo dev).
4. **Usuario QA existente con `TenantId` incorrecto** — **Corrección:** actualización de `TenantId` en rama existente de `SeedQaTenantRoleUsersAsync`.
