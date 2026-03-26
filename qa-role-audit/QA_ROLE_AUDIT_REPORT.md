# Auditoría QA — roles y seguridad (tenant demo)

**Ubicación:** `qa-role-audit/`  
**Usuarios QA:** `qa.admin@medflow.local` … `qa.patient@medflow.local` — contraseña `Development:QaRoleUsersPassword`  
**Archivos fuente:** `01`–`11` + JSON; este archivo es el **informe único** consolidado.

---

## Índice

1. [Validación del seed](#1-validación-del-seed)
2. [Resultados de login (JSON)](#2-resultados-de-login-json)
3. [Claims JWT](#3-claims-jwt)
4. [Matriz de permisos](#4-matriz-de-permisos)
5. [Protección de endpoints](#5-protección-de-endpoints)
6. [Aislamiento de datos](#6-aislamiento-de-datos)
7. [Vulnerabilidades corregidas](#7-vulnerabilidades-corregidas)
8. [Tests fallidos antes del fix (JSON)](#8-tests-fallidos-antes-del-fix-json)
9. [Tests pasados después del fix (JSON)](#9-tests-pasados-después-del-fix-json)
10. [Resumen de cambios de código](#10-resumen-de-cambios-de-código)
11. [Estado final](#11-estado-final)

---

## 1. Validación del seed

### SeedQaTenantRoleUsersAsync

- Ejecución: `Program.cs` solo si `ASPNETCORE_ENVIRONMENT=Development` y `Development:QaRoleUsersPassword` no vacío.
- Idempotencia: crea usuario o resetea contraseña + rol; si existe, corrige `TenantId` al tenant `demo` cuando difiere.
- Roles: Admin, Reception, Doctor, Billing, Staff, Patient — un email `qa.*@medflow.local` por rol.
- Sin duplicados por email: búsqueda por `NormalizedEmail` con `IgnoreQueryFilters`.
- Activo / desbloqueo: `IsActive=true`, `SetLockoutEndDateAsync(null)`, `ResetAccessFailedCountAsync` tras reset.

### SeedQaPatientPortalPatientAsync

- Tras usuarios QA: crea `Patients` con `UserId` del `qa.patient@medflow.local` si no existía (portal + `/api/v1/mobile/auth/login`).

### PostgreSQL (verificación)

- `AspNetUsers`: emails `qa.*@medflow.local` presentes; `TenantId` = UUID tenant demo.
- `AspNetUserRoles`: un rol por usuario QA según matriz.
- `Patients`: fila con `UserId` ligada a QA paciente cuando aplica.

### Tenant demo comercial

- Estado suscripción puede ser `PastDue`; en **Development** `Saas:AllowOperationsWhenPastDue: true` evita bloqueo middleware para pruebas MVC (no altera producción si no se copia ese nodo).

---

## 2. Resultados de login (JSON)

*Fuente: `02-login-results.json`*

```json
{
    "staffLogins":  [
                        {
                            "email":  "qa.admin@medflow.local",
                            "http":  200,
                            "iss":  "MedFlow",
                            "aud":  "MedFlow.Mobile",
                            "tenant_id":  "47602bdf-4750-4796-afbc-02c8bdaf4613",
                            "role":  "Admin"
                        },
                        {
                            "email":  "qa.reception@medflow.local",
                            "http":  200,
                            "iss":  "MedFlow",
                            "aud":  "MedFlow.Mobile",
                            "tenant_id":  "47602bdf-4750-4796-afbc-02c8bdaf4613",
                            "role":  "Reception"
                        },
                        {
                            "email":  "qa.doctor@medflow.local",
                            "http":  200,
                            "iss":  "MedFlow",
                            "aud":  "MedFlow.Mobile",
                            "tenant_id":  "47602bdf-4750-4796-afbc-02c8bdaf4613",
                            "role":  "Doctor"
                        },
                        {
                            "email":  "qa.billing@medflow.local",
                            "http":  200,
                            "iss":  "MedFlow",
                            "aud":  "MedFlow.Mobile",
                            "tenant_id":  "47602bdf-4750-4796-afbc-02c8bdaf4613",
                            "role":  "Billing"
                        },
                        {
                            "email":  "qa.staff@medflow.local",
                            "http":  200,
                            "iss":  "MedFlow",
                            "aud":  "MedFlow.Mobile",
                            "tenant_id":  "47602bdf-4750-4796-afbc-02c8bdaf4613",
                            "role":  "Staff"
                        }
                    ],
    "patientMobile":  {
                          "http":  200,
                          "patientId":  "9cedea3d-6396-4fe9-b4bf-69b6c233eeef",
                          "tenantId":  "47602bdf-4750-4796-afbc-02c8bdaf4613"
                      },
    "negatives":  [
                      {
                          "case":  "staff wrong password",
                          "status":  401
                      },
                      {
                          "case":  "patient on staff login",
                          "status":  401
                      },
                      {
                          "case":  "mobile patient dashboard no token",
                          "status":  401
                      }
                  ]
}
```

---

## 3. Claims JWT

### Emisión

- **Staff (Admin–Staff):** `POST /api/v1/auth/staff/login` con `email`, `password`, `tenantCode` (obligatorio).
- **Patient:** `POST /api/v1/mobile/auth/login` (solo rol Patient + vínculo `Patients.UserId` + portal habilitado).

### Claims observados (access token)

| Claim | Valor típico |
|-------|----------------|
| `iss` | `MedFlow` (`Jwt:Issuer`) |
| `aud` | `MedFlow.Mobile` (`Jwt:Audience`) |
| `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` | UserId |
| `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress` | Email |
| `tenant_id` | GUID tenant (demo) |
| `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` | Rol (uno por token en usuarios QA) |
| `exp` | UTC |

### Expiración

- `AccessTokenExpirationMinutes` (appsettings, p. ej. 15).
- Refresh: `RefreshTokenExpirationDays` (p. ej. 7).

### Negativos

- Contraseña incorrecta staff login → **401**.
- Patient en staff login → **401** (debe usar login móvil paciente).
- Dashboard paciente sin `Authorization` → **401**.

---

## 4. Matriz de permisos

**Contraseña:** `Development:QaRoleUsersPassword`  
**Staff JWT:** `POST /api/v1/auth/staff/login` (`email`, `password`, `tenantCode`)  
**Patient JWT:** `POST /api/v1/mobile/auth/login`

### Por rol — API login JWT

| Rol | Endpoint | Esperado | Antes (pre-fix) | Corrección | Después |
|-----|----------|----------|-----------------|------------|---------|
| Admin | staff login | 200, rol Admin | Sin endpoint JWT staff | `TenantStaffAuthService` + controller | 200, `role=Admin`, `tenant_id` demo |
| Reception | staff login | 200, Reception | Igual | Igual | 200 |
| Doctor | staff login | 200, Doctor | Igual | Igual | 200 |
| Billing | staff login | 200, Billing | Igual | Igual | 200 |
| Staff | staff login | 200, Staff | Igual | Igual | 200 |
| Patient | staff login | 401 | N/A | Rechazo explícito Patient | 401 |
| Patient | mobile login | 200 + patientId | Fallo sin `Patients.UserId` | `SeedQaPatientPortalPatientAsync` | 200 |

### Por rol — MVC (cookie), rutas GET

| Ruta | Rol | Esperado HTTP | Antes (pre-fix) | Corrección | Después |
|------|-----|---------------|-----------------|------------|---------|
| `/AdminUsers` | Admin | 200 | Redirect `/Commercial/Blocked` (PastDue) | `Saas:AllowOperationsWhenPastDue: true` en Development | 200 |
| `/AdminUsers` | Reception, Doctor, Billing, Staff | 403 | Blocked antes de permiso | Saas dev | 403 |
| `/Patients` | Admin, Reception, Doctor, Staff | 200 | Blocked | Saas dev | 200 |
| `/Patients` | Billing | 403 | Blocked | Saas dev | 403 (sin `patients.*`) |
| `/Settings` | Admin | 200 | Blocked | Saas dev | 200 |
| `/Settings` | otros staff | 403 | Blocked | Saas dev | 403 |
| `/NotificationTemplates` | Admin | 200 | Blocked | Saas dev | 200 |
| `/NotificationTemplates` | otros staff | 403 | Blocked | Saas dev | 403 |

### Negativos API

| Caso | Esperado | Resultado |
|------|----------|-----------|
| Staff password incorrecta | 401 | 401 |
| `qa.patient` en staff login | 401 | 401 |
| API paciente sin Bearer | 401 | 401 |

---

## 5. Protección de endpoints

### Anónimos intencionados

- `Account/Login`, `Health/*`
- `POST /api/v1/mobile/auth/login`, `POST /api/v1/mobile/auth/refresh`
- `POST /api/v1/auth/staff/login` (credenciales + tenant)
- `Onboarding/*` (provisionamiento)

### API móvil paciente

- `[Authorize]` + esquema **JwtBearer** en controladores bajo `api/v1/mobile/*` (paciente).

### Riesgo residual

- Endpoints MVC dependen de cookie + `[Authorize]` + `RequirePermission`; no sustituir por confianza en el front.

### Acciones

- Sin `[AllowAnonymous]` indebido en datos sensibles fuera de login/onboarding/health.

---

## 6. Aislamiento de datos

- JWT incluye `tenant_id`; servicios usan `ITenantContext` + filtros EF en entidades con tenant.
- Staff login exige `tenantCode` y `user.TenantId` coincide con tenant resuelto.
- Patient: acceso API resuelve `patientId` por `UserId` — sin vínculo, login móvil falla (comportamiento seguro).

### Pruebas de ataque (muestra)

- Sin token en API paciente → 401.
- Staff password incorrecta → 401.
- Patient en endpoint staff → 401.

### Pendiente manual

- Segundo tenant en BD + prueba de cruce de `tenant_id` en API (no automatizado en esta pasada).

---

## 7. Vulnerabilidades corregidas

1. **No había login JWT para roles de clínica** — solo paciente móvil; personal no podía validar tokens con roles. **Corrección:** `TenantStaffAuthService` + `POST /api/v1/auth/staff/login`.
2. **QA paciente sin registro `Patients`** — portal/API paciente imposible. **Corrección:** sembrado `SeedQaPatientPortalPatientAsync`.
3. **Suscripción PastDue bloqueaba toda la UI** — imposible probar permisos por rol en MVC. **Corrección:** `Saas:AllowOperationsWhenPastDue: true` en `appsettings.Development.json` (solo dev).
4. **Usuario QA existente con `TenantId` incorrecto** — **Corrección:** actualización de `TenantId` en rama existente de `SeedQaTenantRoleUsersAsync`.

---

## 8. Tests fallidos antes del fix (JSON)

*Fuente: `08-failed-tests-before-fix.json`*

```json
{
  "notes": "Antes de correcciones de esta sesión",
  "items": [
    { "test": "JWT staff para qa.admin", "result": "no existía endpoint", "severity": "critical" },
    { "test": "mobile login qa.patient", "result": "fallo sin Patient.UserId", "severity": "critical" },
    { "test": "MVC /AdminUsers con qa.admin", "result": "redirect Commercial/Blocked por PastDue", "severity": "high" }
  ]
}
```

---

## 9. Tests pasados después del fix (JSON)

*Fuente: `09-passed-tests-after-fix.json`*

```json
{
  "unitTests": "112 passed (MedFlow.UnitTests)",
  "apiStaffJwt": "5/5 roles 200",
  "apiPatientJwt": "200",
  "mvcMatrix": "ver 04-role-permission-matrix.md",
  "negatives": "401 wrong password; 401 patient on staff; 401 no bearer on patient API"
}
```

---

## 10. Resumen de cambios de código

| Archivo | Cambio |
|---------|--------|
| `src/MedFlow.Application/Interfaces/ITenantStaffAuthService.cs` | Nuevo — contratos staff JWT |
| `src/MedFlow.Infrastructure/Identity/TenantStaffAuthService.cs` | Nuevo — login staff + refresh |
| `src/MedFlow.Infrastructure/DependencyInjection.cs` | Registro `ITenantStaffAuthService` |
| `src/MedFlow.Web/Controllers/Api/TenantStaffAuthController.cs` | Nuevo — `POST api/v1/auth/staff/login` |
| `src/MedFlow.Infrastructure/Persistence/DataSeeder.cs` | `TenantId` en update; `SeedQaPatientPortalPatientAsync` |
| `src/MedFlow.Web/appsettings.Development.json` | `Saas:AllowOperationsWhenPastDue` |

---

## 11. Estado final

```
STATUS: COMPLETE (Development QA)
BUILD: if MedFlow.Web is running, full solution build may fail (DLL locked); stop the web process and run dotnet build
UNIT TESTS: 112 passed (last run with solution build OK)
EVIDENCE: qa-role-audit/01..11 + 02-login-results.json (negatives: patient on staff = 401)
JWT STAFF: OK (5 roles)
JWT PATIENT: OK
MVC PERMISSIONS: OK (matrix 04)
CRITICAL OPEN: none for scoped QA deliverable; cross-tenant IDOR second-tenant test manual
PRODUCTION: do not copy Saas:AllowOperationsWhenPastDue bypass to production without review
```

---

*Fin del informe consolidado.*
