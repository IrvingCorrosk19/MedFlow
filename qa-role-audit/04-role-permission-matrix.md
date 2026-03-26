# Matriz de permisos — QA tenant `demo`

**Contraseña:** `Development:QaRoleUsersPassword`  
**Staff JWT:** `POST /api/v1/auth/staff/login` (`email`, `password`, `tenantCode`)  
**Patient JWT:** `POST /api/v1/mobile/auth/login`

## Por rol — API login JWT

| Rol | Endpoint | Esperado | Antes (pre-fix) | Corrección | Después |
|-----|----------|----------|-----------------|------------|---------|
| Admin | staff login | 200, rol Admin | Sin endpoint JWT staff | `TenantStaffAuthService` + controller | 200, `role=Admin`, `tenant_id` demo |
| Reception | staff login | 200, Reception | Igual | Igual | 200 |
| Doctor | staff login | 200, Doctor | Igual | Igual | 200 |
| Billing | staff login | 200, Billing | Igual | Igual | 200 |
| Staff | staff login | 200, Staff | Igual | Igual | 200 |
| Patient | staff login | 401 | N/A | Rechazo explícito Patient | 401 |
| Patient | mobile login | 200 + patientId | Fallo sin `Patients.UserId` | `SeedQaPatientPortalPatientAsync` | 200 |

## Por rol — MVC (cookie), rutas GET

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

## Negativos API

| Caso | Esperado | Resultado |
|------|----------|-----------|
| Staff password incorrecta | 401 | 401 |
| `qa.patient` en staff login | 401 | 401 |
| API paciente sin Bearer | 401 | 401 |
