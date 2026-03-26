# Seed QA — validación

## SeedQaTenantRoleUsersAsync

- Ejecución: `Program.cs` solo si `ASPNETCORE_ENVIRONMENT=Development` y `Development:QaRoleUsersPassword` no vacío.
- Idempotencia: crea usuario o resetea contraseña + rol; si existe, corrige `TenantId` al tenant `demo` cuando difiere.
- Roles: Admin, Reception, Doctor, Billing, Staff, Patient — un email `qa.*@medflow.local` por rol.
- Sin duplicados por email: búsqueda por `NormalizedEmail` con `IgnoreQueryFilters`.
- Activo / desbloqueo: `IsActive=true`, `SetLockoutEndDateAsync(null)`, `ResetAccessFailedCountAsync` tras reset.

## SeedQaPatientPortalPatientAsync

- Tras usuarios QA: crea `Patients` con `UserId` del `qa.patient@medflow.local` si no existía (portal + `/api/v1/mobile/auth/login`).

## PostgreSQL (verificación)

- `AspNetUsers`: emails `qa.*@medflow.local` presentes; `TenantId` = UUID tenant demo.
- `AspNetUserRoles`: un rol por usuario QA según matriz.
- `Patients`: fila con `UserId` ligada a QA paciente cuando aplica.

## Tenant demo comercial

- Estado suscripción puede ser `PastDue`; en **Development** `Saas:AllowOperationsWhenPastDue: true` evita bloqueo middleware para pruebas MVC (no altera producción si no se copia ese nodo).
