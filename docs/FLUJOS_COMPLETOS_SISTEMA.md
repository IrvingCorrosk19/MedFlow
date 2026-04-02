# FLUJOS COMPLETOS DEL SISTEMA — MedFlow AI

> Documentación técnica y funcional de todos los flujos de usuario del sistema.
> Generada por análisis directo del código fuente.
> Fecha: 2026-04-02

---

## ÍNDICE

1. [Autenticación y Acceso](#1-autenticación-y-acceso)
2. [Gestión de Pacientes](#2-gestión-de-pacientes)
3. [Gestión de Doctores](#3-gestión-de-doctores)
4. [Citas (Appointments)](#4-citas-appointments)
5. [Registros Médicos](#5-registros-médicos)
6. [Facturación — Facturas](#6-facturación--facturas)
7. [Facturación — Pagos](#7-facturación--pagos)
8. [Movimientos de Caja](#8-movimientos-de-caja)
9. [Reportes](#9-reportes)
10. [Analítica (Analytics)](#10-analítica-analytics)
11. [Automatizaciones y Workflows](#11-automatizaciones-y-workflows)
12. [Módulo de IA](#12-módulo-de-ia)
13. [Portal del Paciente](#13-portal-del-paciente)
14. [Administración de Usuarios](#14-administración-de-usuarios)
15. [Administración de Roles y Permisos](#15-administración-de-roles-y-permisos)
16. [Plantillas de Notificación](#16-plantillas-de-notificación)
17. [Configuración (Settings)](#17-configuración-settings)
18. [Registro de Eventos (Audit)](#18-registro-de-eventos-audit)
19. [Onboarding de Clínica](#19-onboarding-de-clínica)
20. [SuperAdmin — Tenants](#20-superadmin--tenants)
21. [SuperAdmin — Planes y Suscripciones](#21-superadmin--planes-y-suscripciones)
22. [SuperAdmin — Billing SaaS](#22-superadmin--billing-saas)
23. [Ops / Monitoreo](#23-ops--monitoreo)
24. [API Móvil](#24-api-móvil)
25. [Webhooks e Integraciones](#25-webhooks-e-integraciones)
26. [Mapeo de Permisos](#26-mapeo-de-permisos)

---

## Arquitectura General

- **Framework:** ASP.NET Core MVC + Razor Views
- **Autenticación:** ASP.NET Core Identity (Staff/Portal) + JWT (API Móvil)
- **Autorización:** Attribute `[RequirePermission(PermissionCodes.X)]` + `[Authorize(Roles)]`
- **Multitenancy:** `TenantResolutionMiddleware` → `ITenantContext` inyectado en cada request
- **Feature flags:** `TenantCommercialMiddleware` + `[RequirePlanFeature("X")]`
- **Middleware stack:** GlobalExceptionHandling → SecurityHeaders → RateLimiting → TenantResolution → TenantCommercial → RequestLogging

---

## 1. Autenticación y Acceso

### 1.1 Login de Staff

| | |
|---|---|
| **Ruta** | `GET /Account/Login` · `POST /Account/Login` |
| **Permiso** | `[AllowAnonymous]` |
| **Controlador** | `AccountController` |

**Flujo:**
1. GET → Vista con `AccountLoginViewModel { Email, Password, RememberMe, ReturnUrl }`
2. POST → `SignInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: true)`
3. Si `Succeeded`:
   - Verifica que el usuario NO sea solo rol `Patient` (rechaza con mensaje)
   - Redirige a `returnUrl` (validado con `Url.IsLocalUrl`) o a `Dashboard/Index`
4. Si `IsLockedOut` → Error "Cuenta bloqueada por intentos fallidos"
5. Si `IsNotAllowed` → Error "No se permite el inicio de sesión"
6. Fallo genérico → Error "Correo o contraseña incorrectos"

**Validaciones:** ModelState, lockout automático por Identity, rol mínimo no sea Patient.

---

### 1.2 Logout de Staff

| | |
|---|---|
| **Ruta** | `POST /Account/Logout` |
| **Permiso** | `[Authorize]` + `[ValidateAntiForgeryToken]` |

**Flujo:** `SignInManager.SignOutAsync()` → Redirect a `Login`.

---

### 1.3 Recuperación de Contraseña (Staff)

| | |
|---|---|
| **Rutas** | `GET/POST /Account/ForgotPassword` · `GET/POST /Account/ResetPassword` |
| **Permiso** | `[AllowAnonymous]` |

**Flujo ForgotPassword:**
1. GET → Vista con `ForgotPasswordViewModel { Email }`
2. POST → `UserManager.FindByEmailAsync(email)`
   - Si usuario no existe o correo no confirmado → Redirige igual (no revela existencia)
   - Si existe → `GeneratePasswordResetTokenAsync(user)` → construye URL con `token + email`
   - En desarrollo: `TempData["ResetLink"] = callbackUrl` (en producción enviar por email)
3. Redirect a `ForgotPasswordConfirmation`

**Flujo ResetPassword:**
1. GET con `?token=...&email=...` → Vista con `ResetPasswordViewModel`
2. POST → `UserManager.ResetPasswordAsync(user, token, password)`
3. Éxito → Redirect a `ResetPasswordConfirmation`
4. Error → Muestra errores de Identity (token expirado, política de contraseña)

**Validaciones:** Email válido, token no expirado, nueva contraseña ≥ 6 chars con mayúscula + minúscula + dígito.

---

### 1.4 Login de Paciente (Portal)

| | |
|---|---|
| **Ruta** | `GET/POST /PatientPortal/Auth/Login` |
| **Área** | PatientPortal |
| **Permiso** | `[AllowAnonymous]` |
| **Controlador** | `Areas/PatientPortal/Controllers/AuthController` |

**Flujo:**
1. GET → Vista con `PatientLoginViewModel { Email, Password, RememberMe }`
2. POST → `SignInManager.PasswordSignInAsync(...)`
3. Verifica que usuario tenga rol `Patient` (rechaza staff)
4. Verifica que el portal esté habilitado: `IPatientPortalService.GetOptionsAsync(tenantId)`
5. Verifica que el `tenant_id` del claim coincida con el tenant del contexto
6. Éxito → Redirect a `PatientPortal/Home`

---

### 1.5 Logout de Paciente

| | |
|---|---|
| **Ruta** | `POST /PatientPortal/Auth/Logout` |
| **Autorización** | `[PatientPortalAuthorize]` |

**Flujo:** `SignOutAsync()` → Redirect a `Auth/Login`.

---

### 1.6 Login Mobile (API)

| | |
|---|---|
| **Ruta** | `POST /api/v1/mobile/auth/login` |
| **Permiso** | `[AllowAnonymous]` |
| **Controlador** | `Controllers/Api/Mobile/V1/MobileAuthController` |

**Request:** `{ "email": "...", "password": "..." }`

**Flujo:**
1. `IMobileAuthService.LoginAsync(request, ct)`
2. Éxito → HTTP 200 + `{ accessToken, refreshToken, expiresIn }`
3. Fallo → HTTP 401

---

### 1.7 Refresh Token Mobile

| | |
|---|---|
| **Ruta** | `POST /api/v1/mobile/auth/refresh` |

**Request:** `{ "refreshToken": "..." }`

**Flujo:** `IMobileAuthService.RefreshAsync(refreshToken)` → Nuevos tokens o HTTP 401.

---

### 1.8 Logout Mobile

| | |
|---|---|
| **Ruta** | `POST /api/v1/mobile/auth/logout` |
| **Autorización** | JWT Bearer |

**Flujo:** Invalida refresh token → HTTP 204.

---

## 2. Gestión de Pacientes

### 2.1 Listar Pacientes

| | |
|---|---|
| **Ruta** | `GET /Patients` |
| **Permiso** | `patients.view` |
| **Parámetros** | `search` (string), `estadoActivo` (bool?) |

**Flujo:**
1. `IPatientService.GetAllAsync(search, estadoActivo)`
2. Retorna lista con nombre, documento, teléfono, correo, estado, acciones.

---

### 2.2 Ver Detalle de Paciente

| | |
|---|---|
| **Ruta** | `GET /Patients/Details/{id}` |
| **Permiso** | `patients.view` |

**Flujo:** `GetByIdAsync(id)` → Vista con datos clínicos, estado portal, accesos rápidos (citas, expedientes, facturas).

---

### 2.3 Crear Paciente

| | |
|---|---|
| **Ruta** | `GET/POST /Patients/Create` |
| **Permiso** | `patients.create` |

**GET:** Vista vacía con `PatientViewModel`.

**POST — campos:**
- Nombre(s), apellido(s), fecha nacimiento, sexo
- Tipo y número de documento
- Teléfono, correo, dirección
- Contacto emergencia (nombre + teléfono)
- Alergias, observaciones
- `IsActive` (default: true)

**Validaciones:**
- Nombre y apellido requeridos
- Fecha nacimiento: no futura, no > 150 años atrás
- Email válido si se proporciona

**Flujo POST:**
1. `IPatientService.CreateAsync(patient)` → `(bool ok, string? error)`
2. Éxito → `TempData["Success"]` + Redirect `Index`
3. Error → Vista con mensaje

---

### 2.4 Editar Paciente

| | |
|---|---|
| **Ruta** | `GET/POST /Patients/Edit/{id}` |
| **Permiso** | `patients.edit` |

**Flujo GET:** Precarga datos del paciente.
**Flujo POST:** Mismas validaciones que Create + `IPatientService.UpdateAsync(patient)`.

---

### 2.5 Eliminar Paciente

| | |
|---|---|
| **Ruta** | `POST /Patients/Delete/{id}` |
| **Permiso** | `patients.delete` |

**Flujo:**
1. `IPatientService.DeleteAsync(id)` envuelto en try/catch
2. Si FK constraint → `TempData["Error"]` = "No se puede eliminar..."
3. Éxito → Redirect `Index`

---

### 2.6 Exportar Pacientes (CSV)

| | |
|---|---|
| **Ruta** | `GET /Patients/ExportCsv?search=&estadoActivo=` |
| **Permiso** | `patients.view` |

**Flujo:** `GetAllAsync(search, estadoActivo)` → genera CSV con BOM UTF-8 → `File(bytes, "text/csv", "pacientes_{date}.csv")`.

---

### 2.7 Habilitar Portal del Paciente

| | |
|---|---|
| **Ruta** | `POST /Patients/EnablePortal/{id}` |
| **Permiso** | `patients.edit` |

**Flujo:**
1. `IPatientPortalEnableService.EnablePortalForPatientAsync(id, password?)`
2. Crea user en Identity con rol `Patient` si no existe
3. Éxito → `TempData["Success"]` con contraseña temporal
4. Redirect `Details/{id}`

---

### 2.8 Deshabilitar Portal del Paciente

| | |
|---|---|
| **Ruta** | `POST /Patients/DisablePortal/{id}` |
| **Permiso** | `patients.edit` |

**Flujo:** `IPatientPortalEnableService.DisablePortalForPatientAsync(id)` → Desactiva user de Identity.

---

## 3. Gestión de Doctores

### 3.1 Listar Doctores

| | |
|---|---|
| **Ruta** | `GET /Doctors` |
| **Permiso** | `doctors.view` |
| **Parámetros** | `search`, `isActive`, `specialty` |

**Flujo:** `IDoctorService.GetAllAsync(search, isActive)` → filtra por especialidad en memoria → vista con SelectList de especialidades.

---

### 3.2 Ver Detalle Doctor

| | |
|---|---|
| **Ruta** | `GET /Doctors/Details/{id}` |
| **Permiso** | `doctors.view` |

**Flujo:** `IDoctorService.GetByIdAsync(id)` → vista con especialidad, contacto, estado, citas asociadas.

---

### 3.3 Crear Doctor

| | |
|---|---|
| **Ruta** | `GET/POST /Doctors/Create` |
| **Permiso** | `doctors.create` |

**Campos:** `FirstName`, `LastName`, `Speciality`, `LicenseNumber`, `Phone`, `Email`, `ConsultationRoom`, `IsActive`.

---

### 3.4 Editar Doctor

| | |
|---|---|
| **Ruta** | `GET/POST /Doctors/Edit/{id}` |
| **Permiso** | `doctors.edit` |

---

### 3.5 Eliminar Doctor

| | |
|---|---|
| **Ruta** | `POST /Doctors/Delete/{id}` |
| **Permiso** | `doctors.delete` |

**Validación:** Verifica citas activas o expedientes asociados antes de eliminar.

---

### 3.6 Exportar Doctores (CSV)

| | |
|---|---|
| **Ruta** | `GET /Doctors/ExportCsv` |
| **Permiso** | `doctors.view` |

**Formato CSV:** Nombre, Especialidad, Licencia, Teléfono, Correo, Activo.

---

## 4. Citas (Appointments)

### 4.1 Listar Citas

| | |
|---|---|
| **Ruta** | `GET /Appointments` |
| **Permiso** | `appointments.view` |
| **Parámetros** | `from`, `to`, `doctorId`, `patientId`, `status` |

**Flujo:**
1. Defaults: `from` = hoy, `to` = hoy +7 días
2. `IAppointmentService.GetAllAsync(from, to, doctorId, patientId)`
3. Filtra por `status` si se indica
4. Obtiene `SelectList` de doctores para filtro
5. Vista con DataTable + badges de estado por color

---

### 4.2 Ver Detalle de Cita

| | |
|---|---|
| **Ruta** | `GET /Appointments/Details/{id}` |
| **Permiso** | `appointments.view` |

**Muestra:** Paciente, doctor, fecha/hora, sala, motivo, estado, acciones rápidas de estado.

---

### 4.3 Crear Cita

| | |
|---|---|
| **Ruta** | `GET/POST /Appointments/Create` |
| **Permiso** | `appointments.create` |

**Campos:** `PatientId`, `DoctorId`, `ScheduledDate`, `StartTime`, `EndTime`, `Reason`, `Notes`, `ConsultationRoom`, `Status`.

**Validaciones:**
- `EndTime > StartTime`
- `ScheduledDate >= DateTime.Today`
- Paciente y doctor activos
- `IAppointmentService.HasConflictAsync(doctorId, date, start, end)` → error si hay solapamiento

**Flujo POST:**
1. Validaciones arriba
2. `IAppointmentService.CreateAsync(appointment)`
3. Éxito → Redirect `Index`

---

### 4.4 Editar Cita

| | |
|---|---|
| **Ruta** | `GET/POST /Appointments/Edit/{id}` |
| **Permiso** | `appointments.edit` |

Mismo flujo que Create con precarga de datos. Validaciones de tiempo aplican igual.

---

### 4.5 Confirmar Cita

| | |
|---|---|
| **Ruta** | `POST /Appointments/Confirm/{id}` |
| **Permiso** | `appointments.edit` |

**Flujo:** `appointment.Status = Confirmed` → `UpdateAsync()` → Redirect `Index`.

---

### 4.6 Marcar Cita como Completada

| | |
|---|---|
| **Ruta** | `POST /Appointments/MarkCompleted/{id}` |
| **Permiso** | `appointments.edit` |

**Flujo:** `appointment.Status = Completed` → `UpdateAsync()` → Redirect `Details`.

---

### 4.7 Registrar No-Show

| | |
|---|---|
| **Ruta** | `POST /Appointments/MarkNoShow/{id}` |
| **Permiso** | `appointments.edit` |

**Flujo:** `appointment.Status = NoShow` → `UpdateAsync()` → Redirect `Details`.

---

### 4.8 Cancelar / Eliminar Cita

| | |
|---|---|
| **Ruta** | `POST /Appointments/Delete/{id}` |
| **Permiso** | `appointments.cancel` |

**Flujo:**
1. `IAppointmentService.DeleteAsync(id)` en try/catch
2. Error → `TempData["Error"]` = "No se pudo eliminar la cita"
3. Éxito → Redirect `Index`

---

## 5. Registros Médicos

### 5.1 Ver Historial Médico del Paciente

| | |
|---|---|
| **Ruta** | `GET /MedicalRecords/Patient/{patientId}` |
| **Permiso** | `medical_records.view` |

**Flujo:**
1. Obtiene paciente (`IPatientService.GetByIdAsync`)
2. `IMedicalRecordService.GetHistoryByPatientAsync(patientId)` → lista cronológica
3. Vista con opciones de agregar/ver/editar registro

---

### 5.2 Ver Detalle de Registro Médico

| | |
|---|---|
| **Ruta** | `GET /MedicalRecords/Details/{id}` |
| **Permiso** | `medical_records.view` |

**Muestra:** Fecha consulta, doctor, diagnóstico, tratamiento, notas, signos vitales, prescripciones, adjuntos.

---

### 5.3 Crear Registro Médico

| | |
|---|---|
| **Ruta** | `GET/POST /MedicalRecords/Create?patientId={id}&appointmentId={id}` |
| **Permiso** | `medical_records.create` |

**Campos:**
- `PatientId`, `DoctorId`, `AppointmentId` (opcional)
- `VisitDate`, `VisitTime`
- Signos vitales: `HeightCm`, `WeightKg`, `BloodPressure`, `HeartRateBpm`, `TemperatureCelsius`
- `Diagnosis` (diagnóstico)
- `Treatment` (tratamiento)
- `Notes`, `Observations`
- Prescripciones: tabla de filas `{MedicationName, Dose, Frequency, Duration}`

**Validaciones:**
- Campos de diagnóstico requeridos
- `AppointmentId` si se proporciona → debe pertenecer al mismo paciente
- Prescripciones: `MedicationName` no vacío si se agrega fila

**Flujo POST:** `IMedicalRecordService.CreateAsync(record)` en try/catch → Redirect `Patient/{patientId}`.

---

### 5.4 Editar Registro Médico

| | |
|---|---|
| **Ruta** | `GET/POST /MedicalRecords/Edit/{id}` |
| **Permiso** | `medical_records.edit` |

Mismo flujo que Create con precarga de datos.

---

### 5.5 Eliminar Registro Médico

| | |
|---|---|
| **Ruta** | `POST /MedicalRecords/Delete/{id}` |
| **Permiso** | `medical_records.delete` |

---

### 5.6 Cargar Adjunto

| | |
|---|---|
| **Ruta** | `POST /MedicalRecords/UploadAttachment` |
| **Permiso** | `medical_records.edit` |

**Validaciones:**
- Extensión permitida: `.pdf`, `.jpg`, `.jpeg`, `.png`, `.doc`, `.docx`
- Límite de tamaño: 50 MB (global `RequestSizeLimit`)
- Verifica directorio destino antes de escribir

---

## 6. Facturación — Facturas

### 6.1 Listar Facturas

| | |
|---|---|
| **Ruta** | `GET /BillingInvoices` |
| **Permiso** | `billing.view` |
| **Feature** | `Billing` |
| **Parámetros** | `patientId`, `from`, `to`, `status` |

**Flujo:** `IBillingInvoiceService.SearchAsync(...)` → lista con estado, monto, saldo pendiente, indicador de vencida.

---

### 6.2 Ver Detalle de Factura

| | |
|---|---|
| **Ruta** | `GET /BillingInvoices/Details/{id}` |
| **Permiso** | `billing.view` |

**Muestra:** Número, fecha, paciente, conceptos, totales, pagos registrados, botones de registrar pago / anular (condicional).

---

### 6.3 Crear Factura

| | |
|---|---|
| **Ruta** | `GET/POST /BillingInvoices/Create?patientId=&appointmentId=` |
| **Permiso** | `billing.create` |
| **Feature** | `Billing` |

**Campos:**
- `PatientId`, `DoctorId`, `AppointmentId` (opcional)
- `IssueDate`, `DueDate`
- Líneas: `{ ItemType, Description, Quantity, UnitPrice }`
- `DiscountAmount`, `Notes`

**Validaciones:**
- `DueDate >= IssueDate`
- Líneas: `Quantity > 0` y `UnitPrice > 0`
- Total > 0
- `DiscountAmount <= Subtotal`

**Flujo POST:** `IBillingInvoiceService.CreateAsync(invoice)` en try/catch.

---

### 6.4 Imprimir Factura

| | |
|---|---|
| **Ruta** | `GET /BillingInvoices/Print/{id}` |
| **Permiso** | `billing.view` |

**Flujo:** Vista sin layout (`Layout = null`) optimizada para impresión / PDF — incluye datos de clínica, paciente, conceptos, totales, estado de pago.

---

### 6.5 Registrar Pago desde Factura

| | |
|---|---|
| **Ruta** | `POST /BillingInvoices/RegisterPayment/{id}` |
| **Permiso** | `billing.register_payment` |
| **Feature** | `Billing` |

**Flujo:**
1. Obtiene `userId` del claim (`?.Value` con null check)
2. `IBillingInvoiceService.RegisterPaymentAsync(id, amount, method, date, userId)`
3. Actualiza estado de factura automáticamente (Paid / PartiallyPaid)
4. Redirect `Details/{id}`

---

### 6.6 Anular Factura

| | |
|---|---|
| **Ruta** | `POST /BillingInvoices/CancelInvoice/{id}` |
| **Permiso** | `billing.manage` |

**Validación:** Solo si `AmountPaid == 0`.

---

### 6.7 Anular Pago

| | |
|---|---|
| **Ruta** | `POST /BillingInvoices/CancelPayment/{paymentId}` |
| **Permiso** | `billing.manage` |

**Flujo:**
1. `userId` del claim con null check
2. `IBillingInvoiceService.CancelPaymentAsync(paymentId, userId)`
3. Recalcula estado de factura
4. Redirect `Details/{invoiceId}`

---

## 7. Facturación — Pagos

### 7.1 Listar Pagos

| | |
|---|---|
| **Ruta** | `GET /Payments` |
| **Permiso** | `billing.view` |
| **Feature** | `Billing` |
| **Parámetros** | `invoiceId`, `patientId`, `from`, `to`, `method` |

**Flujo:** `IPaymentService.SearchAsync(...)` → tabla con monto, método, factura, paciente.

---

### 7.2 Ver Detalle de Pago

| | |
|---|---|
| **Ruta** | `GET /Payments/Details/{id}` |
| **Permiso** | `billing.view` |

**Muestra:** Monto, método, fecha, factura asociada, usuario que registró.

---

### 7.3 Registrar Pago (flujo independiente)

| | |
|---|---|
| **Ruta** | `GET/POST /Payments/Create?billingInvoiceId={id}` |
| **Permiso** | `payments.create` |
| **Feature** | `Billing` |

**GET:**
- Si `billingInvoiceId` no existe → `TempData["Error"]` + Redirect `Index`
- Si existe → precarga `PatientId`, `BalanceDue` en vista

**Campos:** `BillingInvoiceId`, `PatientId`, `Amount`, `PaymentDate`, `PaymentMethod`.

**Validaciones:**
- `Amount > 0`
- `Amount <= BalanceDue`
- `PaymentDate` válida

**Flujo POST:**
1. `userId` del claim con null check
2. `IPaymentService.RegisterAsync(invoiceId, amount, method, date, userId)`
3. Éxito → Redirect `Index`

---

## 8. Movimientos de Caja

### 8.1 Listar Movimientos

| | |
|---|---|
| **Ruta** | `GET /CashMovements` |
| **Permiso** | `cash.view` |
| **Parámetros** | `from`, `to`, `type` |

**Flujo:** `ICashMovementService.GetAllAsync(from, to, type)` → tabla con totales acumulados (ingresos, egresos, neto).

---

### 8.2 Crear Movimiento de Caja

| | |
|---|---|
| **Ruta** | `GET/POST /CashMovements/Create` |
| **Permiso** | `cash.create` |

**Campos:** `MovementType` (Ingreso/Egreso), `Amount`, `MovementDate`, `Description`.

**Validaciones:** `Amount > 0` (rango mínimo 0.01).

**Flujo POST:** `ICashMovementService.CreateAsync(movement)` → Redirect `Index`.

---

### 8.3 Eliminar Movimiento de Caja

| | |
|---|---|
| **Ruta** | `POST /CashMovements/Delete/{id}` |
| **Permiso** | `cash.delete` |

---

## 9. Reportes

### 9.1 Reporte de Citas

| | |
|---|---|
| **Ruta** | `GET /Reports/Appointments` |
| **Permiso** | `reports.view` |
| **Parámetros** | `from`, `to`, `doctorId`, `status` |

**Flujo:** `IReportingService.GetAppointmentsReportAsync(filter)` → `AppointmentsReportVm { Rows[], TotalCount, CancelledCount, CompletedCount, NoShowCount }`.

**Validación JS:** `from <= to` en formulario de filtro antes de submit.

---

### 9.2 Reporte Financiero

| | |
|---|---|
| **Ruta** | `GET /Reports/Financial` |
| **Permiso** | `reports.view` |
| **Parámetros** | `from`, `to`, `patientId`, `method` |

**Flujo:** `IReportingService.GetFinancialReportAsync(filter)` → `FinancialReportVm { Rows[], TotalRevenue, TotalPaid, TotalPending }`.

**Validación JS:** `from <= to`.

---

### 9.3 Reporte de Pacientes

| | |
|---|---|
| **Ruta** | `GET /Reports/Patients` |
| **Permiso** | `reports.view` |
| **Parámetros** | `from`, `to`, `includeInactive` |

**Flujo:** `IReportingService.GetPatientsReportAsync(filter)` → `PatientsReportVm { Rows[], NewInPeriod, TopByAppointments[] }`.

**Validación JS:** `from <= to`.

**Export:** `GET /Reports/ExportPatientsCsv?from=&to=&includeInactive=`

---

### 9.4 Reporte de Doctores

| | |
|---|---|
| **Ruta** | `GET /Reports/Doctors` |
| **Permiso** | `reports.view` |
| **Parámetros** | `from`, `to`, `doctorId` |

**Flujo:** `IReportingService.GetDoctorsReportAsync(filter)` → `DoctorsReportVm { Rows[], AvgAppointmentsPerDoctor }`.

**Validación JS:** `from <= to`.

**Export:** `GET /Reports/ExportDoctorsCsv?from=&to=&doctorId=`

---

## 10. Analítica (Analytics)

### 10.1 Dashboard Analítico

| | |
|---|---|
| **Ruta** | `GET /Analytics?from=&to=&days=30` |
| **Permiso** | `reports.view` (clase) |
| **Parámetros** | `from`, `to`, `days` (clamp 7–90) |

**Flujo:**
1. Calcula rango: `toDate = to ?? today`, `fromDate = from ?? toDate - days`
2. Autocorrección: si `fromDate > toDate` → `fromDate = toDate - 30`
3. `IAdvancedAnalyticsService.GetExecutiveAdvancedDashboardAsync(filter)`
4. `ITenantHealthService.GetHealthScoreAsync(tenantId)` → `ViewBag.Health`
5. Vista con gráficos Chart.js (try/catch en controlador)

---

### 10.2 Agregar Snapshot Manual

| | |
|---|---|
| **Ruta** | `POST /Analytics/Aggregate?date={date}` |
| **Permiso** | `settings.manage` |

**Flujo:** `AnalyticsSnapshotProcessorService.ProcessTenantForDateAsync(tenantId, date)` → `TempData["Success"]`.

---

### 10.3 Tendencias

| | |
|---|---|
| **Ruta** | `GET /Analytics/Trends?from=&to=&days=30` |
| **Permiso** | `reports.view` (heredado de clase) |

**Flujo (con try/catch):**
1. Misma lógica de rango que Index
2. `IHistoricalAnalyticsService.GetHistoricalMetricsAsync(filter)`
3. `IPeriodComparisonService.GetPeriodComparisonAsync(from, to)` → `ViewBag.Comparison`
4. Vista con gráficos de tendencia temporal

---

### 10.4 Benchmarking

| | |
|---|---|
| **Ruta** | `GET /Analytics/Benchmarking?cohort=` |
| **Permiso** | `reports.view` (heredado) |

**Flujo (con try/catch):** `IBenchmarkingService.GetBenchmarksAsync(tenantId, cohort)` → Comparativas con cohortes similares.

---

### 10.5 Snapshots

| | |
|---|---|
| **Ruta** | `GET /Analytics/Snapshots?from=&to=` |
| **Permiso** | `reports.view` (heredado) |

**Flujo (con try/catch):** `IHistoricalAnalyticsService.GetSnapshotsAsync(tenantId, from, to)` → Lista de snapshots diarios.

---

### 10.6 Rebuild Analytics

| | |
|---|---|
| **Ruta** | `POST /Analytics/Rebuild` |
| **Permiso** | `settings.manage` |

**Flujo:** `IAnalyticsRebuildService.RebuildAsync(tenantId)` (fire-and-forget) → `TempData["Success"]`.

---

## 11. Automatizaciones y Workflows

### 11.1 Listar Automatizaciones

| | |
|---|---|
| **Ruta** | `GET /Automations` |
| **Permiso** | `automations.view` |
| **Feature** | `Automation` |

**Flujo:** `IWorkflowDefinitionService.ListByTenantAsync(tenantId)` → tabla con estado, métricas (ejecutadas/exitosas/fallidas/tiempo promedio), botón de prueba AJAX.

---

### 11.2 Crear Automatización

| | |
|---|---|
| **Ruta** | `GET/POST /Automations/Create` |
| **Permiso** | `automations.manage` |

**Campos:** `Name`, `Description`, `TriggerEvent`, `WebhookUrl`, `HttpMethod`, `HeadersJson`, `PayloadTemplateJson`, `RetryPolicyJson`, `TimeoutSeconds`, `IsActive`.

**Validaciones:**
- `HeadersJson` → JSON válido si no vacío
- `PayloadTemplateJson` → JSON válido si no vacío
- `RetryPolicyJson` → JSON válido si no vacío
- Código del workflow generado desde `Name` (sanitizado)

---

### 11.3 Editar Automatización

| | |
|---|---|
| **Ruta** | `GET/POST /Automations/Edit/{id}` |
| **Permiso** | `automations.manage` |

Mismas validaciones que Create.

---

### 11.4 Activar / Desactivar Automatización

| | |
|---|---|
| **Ruta** | `POST /Automations/ToggleActive/{id}?isActive={bool}` |
| **Permiso** | `automations.manage` |

---

### 11.5 Eliminar Automatización

| | |
|---|---|
| **Ruta** | `POST /Automations/Delete/{id}` |
| **Permiso** | `automations.manage` |

---

### 11.6 Probar Webhook (AJAX)

| | |
|---|---|
| **Ruta** | `POST /Automations/TestWebhook` |
| **Permiso** | `automations.manage` |

**Flujo AJAX:**
1. Frontend envía `{ id }` vía AJAX con anti-forgery token
2. `IWorkflowTestService.TestAsync(workflowId, tenantId)`
3. Retorna JSON `{ success, statusCode, responseBody, durationMs }`
4. Frontend muestra resultado inline (no recarga página)

---

### 11.7 Listar Ejecuciones de Workflow

| | |
|---|---|
| **Ruta** | `GET /WorkflowExecutions` |
| **Permiso** | `automations.view` |
| **Parámetros** | `workflowId`, `status`, `eventType`, `from`, `to`, `page` |

**Flujo:**
1. `IWorkflowExecutionService.ListAsync(filter)` → lista paginada (50/página)
2. `IWorkflowExecutionService.GetMetricsAsync(tenantId)` → estadísticas globales
3. `IWorkflowDefinitionService.ListByTenantAsync(tenantId)` → SelectList para filtro
4. Vista con tabla y paginación

---

### 11.8 Ver Detalle Ejecución

| | |
|---|---|
| **Ruta** | `GET /WorkflowExecutions/Details/{id}` |
| **Permiso** | `automations.view` |

**Muestra:** Estado, nombre workflow, evento, aggregateId, timestamps, intentos, respuesta HTTP, error completo.

---

### 11.9 Reintentar Ejecución

| | |
|---|---|
| **Ruta** | `POST /WorkflowExecutions/Retry/{id}` |
| **Permiso** | `automations.manage` |

**Validación:** Solo si `Status == WorkflowExecutionStatus.Failed`.

---

### 11.10 Descargar Log de Ejecución

| | |
|---|---|
| **Ruta** | `GET /WorkflowExecutions/DownloadLog/{id}` |
| **Permiso** | `automations.view` |

**Retorna:** `File(content, "text/plain", "execution_{id}.log")` con ID, workflow, evento, timestamps, estado, error.

---

## 12. Módulo de IA

### 12.1 Dashboard de IA

| | |
|---|---|
| **Ruta** | `GET /AI/AIDashboard` |
| **Área** | AI |
| **Permiso** | `ai.insights.view` |

**Muestra:** KPIs clave del tenant, acciones rápidas recomendadas, resumen del día.

---

### 12.2 Copiloto Operativo

| | |
|---|---|
| **Ruta** | `GET /AI/Copilot` · `POST /AI/Copilot/Query` |
| **Permiso** | `ai.insights.view` |

**GET:** Interfaz de chat. No pasa modelo (vista estática con contexto tenant).

**POST `/AI/Copilot/Query` (form: `query`):**
1. Validaciones:
   - `query` no vacía
   - `query.Length <= 500`
   - `tenantId` identificado
2. `IOperationalCopilotService.QueryAsync(tenantId, query, ct)` en try/catch
3. Retorna JSON:
   ```json
   {
     "summary": "...",
     "items": [{ "title", "description", "entityType", "entityId", "actionUrl" }],
     "suggestions": ["...", "..."]
   }
   ```
4. Frontend: spinner durante la llamada → resultado inline → manejo de error AJAX con mensaje

**Seguridad:** Contenido renderizado con jQuery `.text()` (sin `innerHTML` directo) para prevenir XSS.

---

### 12.3 Insights de IA

| | |
|---|---|
| **Ruta** | `GET /AI/Insights` |
| **Permiso** | `ai.insights.view` |
| **Parámetros** | `type`, `minScore`, `minConfidence`, `from`, `to`, `page` |

**Flujo:**
1. `IAIInsightService.GetInsightsAsync(filter)` → lista de insights paginada
2. Insights categorizados: `NoShowRisk`, `PaymentRisk`, `PatientEngagement`

---

### 12.4 Acknowledge / Dismiss Insight

| | |
|---|---|
| **Rutas** | `POST /AI/Insights/Acknowledge/{id}` · `POST /AI/Insights/Dismiss/{id}` |
| **Permiso** | `ai.insights.manage` |

**Flujo:** Actualiza estado del insight → Redirect `Index`.

---

### 12.5 Recomendaciones de IA

| | |
|---|---|
| **Ruta** | `GET /AI/Recommendations` · `POST /AI/Recommendations/Apply/{id}` |
| **Permiso** | `ai.insights.view` / `ai.insights.manage` |

**Flujo:** `IRecommendationEngine.GetRecommendationsAsync()` → lista de recomendaciones operativas. `Apply` ejecuta la acción sugerida.

---

## 13. Portal del Paciente

### 13.1 Home del Paciente

| | |
|---|---|
| **Ruta** | `GET /PatientPortal/inicio` |
| **Autorización** | `[PatientPortalAuthorize]` |
| **Controlador** | `Areas/PatientPortal/Controllers/HomeController` |

**Flujo:**
1. Extrae `patient_id` del claim
2. `IPatientPortalService.GetDashboardAsync(patientId)`
3. Retorna `PatientPortalDashboardDto`:
   - `Profile.FullName`
   - `NextAppointment` (fecha, hora, doctor)
   - `BalanceDue`, `PendingAppointmentsCount`
   - `UnreadNotificationsCount` (conteo real)
4. `HttpContext.Items["UnreadNotificationsCount"]` para layout
5. ViewData["Options"] = `IPatientPortalService.GetOptionsAsync(tenantId)` para visibilidad condicional de módulos

---

### 13.2 Citas del Paciente — Próximas

| | |
|---|---|
| **Ruta** | `GET /PatientPortal/citas` |
| **Controlador** | `Areas/PatientPortal/Controllers/AppointmentsController` |

**Flujo:** `IPatientPortalService.GetUpcomingAppointmentsAsync(patientId)` → lista con opción de cancelar.

**Cancelación:**
- Botón "Cancelar" abre modal Bootstrap
- Modal contiene `<textarea name="motivo">` (opcional)
- Form POST con anti-forgery token
- `POST /PatientPortal/citas/{id}/cancelar?motivo=...` → `IPatientPortalService.CancelAppointmentAsync(patientId, appointmentId, motivo)`

---

### 13.3 Citas del Paciente — Historial

| | |
|---|---|
| **Ruta** | `GET /PatientPortal/citas/historial` |

**Flujo:** `IPatientPortalService.GetAppointmentHistoryAsync(patientId, limit: 50)` → lista cronológica inversa.

---

### 13.4 Detalle de Cita (Portal)

| | |
|---|---|
| **Ruta** | `GET /PatientPortal/citas/{id}` |

**Flujo:** `IPatientPortalService.GetAppointmentDetailAsync(patientId, appointmentId)` → detalle completo (doctor, consultorio, notas, estado).

---

### 13.5 Perfil del Paciente

| | |
|---|---|
| **Ruta** | `GET/POST /PatientPortal/perfil` |
| **Controlador** | `Areas/PatientPortal/Controllers/ProfileController` |

**GET:** Carga datos del paciente.

**POST:** Actualiza teléfono, dirección, contacto de emergencia, alergias. Validaciones básicas de formato.

---

### 13.6 Cambio de Contraseña (Portal)

| | |
|---|---|
| **Ruta** | `GET/POST /PatientPortal/perfil/cambiar-password` |

**Campos:** `CurrentPassword`, `NewPassword` (mín. 6, mayúscula + dígito), `ConfirmNewPassword`.

**Flujo POST:**
1. `UserManager.ChangePasswordAsync(user, currentPassword, newPassword)`
2. Éxito → `TempData["Success"]` + Redirect `Profile/Index`

---

### 13.7 Facturas del Paciente

| | |
|---|---|
| **Ruta** | `GET /PatientPortal/facturas` |
| **Controlador** | `Areas/PatientPortal/Controllers/BillingController` |

**Flujo:**
1. Verifica `options.ShowBilling`
2. `IPatientPortalService.GetInvoicesAsync(patientId)` (todas las facturas)
3. `IPatientPortalService.GetAccountStatusAsync(patientId)` → `(balanceDue, _)`
4. `ViewData["BalanceDue"] = balanceDue`

---

### 13.8 Detalle de Factura (Portal)

| | |
|---|---|
| **Ruta** | `GET /PatientPortal/facturas/{id}` |

**Flujo:** `IPatientPortalService.GetInvoiceAsync(patientId, invoiceId)` → detalle con líneas, pagos, estado.

---

### 13.9 Pagos del Paciente

| | |
|---|---|
| **Ruta** | `GET /PatientPortal/pagos` |

**Flujo:** `GetPaymentsAsync(patientId, limit: 50)` + `GetAccountStatusAsync` → muestra saldo, total pagado, historial.

---

### 13.10 Estado de Cuenta (Portal)

| | |
|---|---|
| **Ruta** | `GET /PatientPortal/estado-cuenta` |

**Flujo:**
1. `GetAccountStatusAsync(patientId)` → `(balanceDue, totalPaid)`
2. `GetInvoicesAsync(patientId)` → todas las facturas
3. Pasa todo vía `ViewData` → vista `AccountStatus.cshtml`

---

### 13.11 Notificaciones del Paciente

| | |
|---|---|
| **Ruta** | `GET /PatientPortal/notificaciones` |
| **Controlador** | `Areas/PatientPortal/Controllers/NotificationsController` |

**Flujo:** `IPatientPortalService.GetNotificationsAsync(patientId)` → lista paginada.

---

### 13.12 Marcar Notificación como Leída

| | |
|---|---|
| **Ruta** | `POST /PatientPortal/notificaciones/{id}/leida` |

**Flujo:**
1. `IPatientPortalService.MarkNotificationReadAsync(patientId, notificationId)`
2. `returnUrl` validado con `Url.IsLocalUrl()` (previene open redirect)
3. Redirect a `returnUrl` o `Index`

---

## 14. Administración de Usuarios

### 14.1 Listar Usuarios

| | |
|---|---|
| **Ruta** | `GET /AdminUsers` |
| **Permiso** | `users.manage` |
| **Rol** | Admin o SuperAdmin |

**Flujo:** `IAdminUserService.GetAllAsync(tenantId)` → tabla con nombre, email, roles, estado, último acceso, acciones.

---

### 14.2 Ver Detalle Usuario

| | |
|---|---|
| **Ruta** | `GET /AdminUsers/Details/{id}` |
| **Permiso** | `users.manage` |

**Muestra:** Datos, roles, estado de bloqueo, enlace de reset de contraseña si se generó.

---

### 14.3 Crear Usuario

| | |
|---|---|
| **Ruta** | `GET/POST /AdminUsers/Create` |
| **Permiso** | `users.manage` |

**Campos:** `Email`, `UserName`, `Password`, `ConfirmPassword`, `FirstName`, `LastName`, `PhoneNumber`, `IsActive`, `RoleNames[]`.

**Validaciones:**
- Password obligatorio (validación adicional en controlador)
- `ConfirmPassword == Password`
- Email único (validado por Identity)

---

### 14.4 Editar Usuario

| | |
|---|---|
| **Ruta** | `GET/POST /AdminUsers/Edit/{id}` |
| **Permiso** | `users.manage` |

**Password opcional en Edit** (solo si se ingresa nuevo valor).

---

### 14.5 Activar / Desactivar Usuario

| | |
|---|---|
| **Ruta** | `POST /AdminUsers/SetActive/{id}?active={bool}` |
| **Permiso** | `users.manage` |

**Flujo:** `IAdminUserService.SetActiveAsync(id, active)`.

---

### 14.6 Desbloquear Usuario

| | |
|---|---|
| **Ruta** | `POST /AdminUsers/UnlockUser/{id}` |
| **Permiso** | `users.manage` |

**Flujo:**
1. `UserManager.SetLockoutEndDateAsync(user, null)` → desbloquea
2. `UserManager.ResetAccessFailedCountAsync(user)` → reinicia contador
3. `TempData["Success"]` + Redirect `Details`

**UI:** Botón visible en `Index` solo cuando `IsLocked = true`.

---

### 14.7 Enviar Enlace de Restablecimiento de Contraseña

| | |
|---|---|
| **Ruta** | `POST /AdminUsers/SendPasswordReset/{id}` |
| **Permiso** | `users.manage` |

**Flujo:**
1. `UserManager.GeneratePasswordResetTokenAsync(user)`
2. Construye URL de reset
3. `TempData["ResetLink"] = resetUrl` (mostrar en Details)
4. En producción: enviar por email

---

## 15. Administración de Roles y Permisos

### 15.1 Listar Roles

| | |
|---|---|
| **Ruta** | `GET /AdminRoles` |
| **Permiso** | `roles.manage` |

**Flujo:** `IRoleAdminService.GetAllAsync(tenantId)` → lista con nombre, descripción, nº de permisos, usuarios asignados.

---

### 15.2 Ver Detalle Rol

| | |
|---|---|
| **Ruta** | `GET /AdminRoles/Details/{id}` |
| **Permiso** | `roles.manage` |

**Muestra:** Nombre, permisos asignados agrupados por módulo, usuarios con este rol.

---

### 15.3 Crear Rol

| | |
|---|---|
| **Ruta** | `GET/POST /AdminRoles/Create` |
| **Permiso** | `roles.manage` |

**Campos:** `Name` (único), `Description`, `IsActive`.

---

### 15.4 Editar Rol

| | |
|---|---|
| **Ruta** | `GET/POST /AdminRoles/Edit/{id}` |
| **Permiso** | `roles.manage` |

**Restricción:** No puede editar roles de sistema (`IsSystem = true`).

---

### 15.5 Eliminar Rol

| | |
|---|---|
| **Ruta** | `POST /AdminRoles/Delete/{id}` |
| **Permiso** | `roles.manage` |

**Validaciones:**
- No puede eliminar roles de sistema
- `UserManager.GetUsersInRoleAsync(role.Name)` → si `Count > 0` → `TempData["Error"]` con mensaje indicando nº de usuarios asignados

**UI:** Confirmación via SweetAlert2 antes del submit.

---

### 15.6 Asignar Permisos a Rol

| | |
|---|---|
| **Ruta** | `GET/POST /AdminRoles/Permissions/{id}` |
| **Permiso** | `roles.manage` |

**GET:**
1. `IPermissionCatalogService.GetAllAsync()` → todos los permisos disponibles
2. `IRoleAdminService.GetByIdAsync(id)` → permisos actuales del rol
3. Vista con checklist agrupado por módulo

**POST:**
1. Lista de `permissionId[]` seleccionados
2. `IRoleAdminService.UpdatePermissionsAsync(roleId, permissionIds)`

**Módulos de permisos disponibles:**
- Patients, Doctors, Appointments, MedicalRecords
- Billing (BillingInvoices + Payments + Cash)
- Users, Roles, Permissions, Audit
- Dashboard, Reports, Settings
- Automations, EventLogs
- AI (Insights, Manage)

---

## 16. Plantillas de Notificación

### 16.1 Listar Plantillas

| | |
|---|---|
| **Ruta** | `GET /NotificationTemplates` |
| **Permiso** | `settings.manage` |

**Flujo:** `INotificationTemplateService.GetAllAsync(tenantId)` → tabla con evento, canal, nombre, estado.

---

### 16.2 Crear Plantilla

| | |
|---|---|
| **Ruta** | `GET/POST /NotificationTemplates/Create` |
| **Permiso** | `settings.manage` |

**Campos:** `EventType`, `Channel`, `Code`, `Name`, `Description`, `IsDefault`.

**Campos condicionales por Channel (visibilidad JS):**
- **Email:** `SubjectTemplate`, `FromEmail`, `FromName`, `BodyTemplate`, `HtmlBodyTemplate`
- **Webhook:** `WebhookUrl`, `WebhookMethod`, `BodyTemplate`
- **InApp:** `BodyTemplate`

**Variables soportadas en templates:** `{{PatientName}}`, `{{AppointmentDate}}`, `{{DoctorName}}`, `{{ClinicName}}`.

---

### 16.3 Editar Plantilla

| | |
|---|---|
| **Ruta** | `GET/POST /NotificationTemplates/Edit/{id}` |
| **Permiso** | `settings.manage` |

Mismo flujo que Create con precarga.

---

## 17. Configuración (Settings)

| | |
|---|---|
| **Ruta** | `GET/POST /Settings` |
| **Permiso** | `settings.manage` |

**Configuraciones del tenant:**
- Nombre de la clínica, logo
- Zona horaria, formato de fecha, idioma, moneda
- Email de notificaciones
- Módulos habilitados (Portal Paciente, Facturación, etc.)
- Configuración de AI Insights (umbral de score, tipos de insight)

---

## 18. Registro de Eventos (Audit)

### 18.1 Ver Event Logs

| | |
|---|---|
| **Ruta** | `GET /EventLogs` |
| **Permiso** | `event_logs.view` |
| **Parámetros** | `entityType`, `action`, `from`, `to`, `userId`, `page` |

**Flujo:** `IEventLogQueryService.QueryAsync(filter)` → tabla paginada con tipo de entidad, acción, actor, timestamp, detalles.

---

### 18.2 Ver Event Log Detail

| | |
|---|---|
| **Ruta** | `GET /EventLogs/Details/{id}` |
| **Permiso** | `event_logs.view` |

**Muestra:** Entidad, acción, usuario, IP, datos anteriores/nuevos (JSON diff).

---

## 19. Onboarding de Clínica

| | |
|---|---|
| **Ruta** | `GET /Onboarding/Step/{step}` |
| **Permiso** | `[AllowAnonymous]` (contexto de setup inicial) |
| **Controlador** | `OnboardingController` |

**Flujo multi-paso (5 steps):**

| Step | Datos | ViewModel |
|---|---|---|
| 1 | Nombre clínica, código (slug), email, teléfono, dirección | `OnboardingStep1Vm` |
| 2 | Selección de plan, inicio de prueba gratuita | `OnboardingStep2Vm` |
| 3 | Admin: nombre, apellido, email, contraseña (+ confirmación) | `OnboardingStep3Vm` |
| 4 | Zona horaria, formato fecha, moneda, idioma | `OnboardingStep4Vm` |
| 5 | Confirmación y provisión | `OnboardingStep5Vm` |

**Indicador de progreso:** `_WizardProgress.cshtml` (partial renderizado en todos los steps).

**Validaciones Step 3 (contraseña):**
- Mínimo 6 caracteres
- Debe contener mayúscula, minúscula y dígito
- Confirmación coincide

**Provisión final (Step 5 POST):**
1. `ITenantProvisioningService.ProvisionAsync(dto)` → Crea tenant, usuario admin, roles iniciales, datos semilla
2. Éxito → Login automático → Redirect `Dashboard`

---

## 20. SuperAdmin — Tenants

### 20.1 Listar Tenants

| | |
|---|---|
| **Ruta** | `GET /SuperAdmin/Tenants` |
| **Área** | SuperAdmin |
| **Rol** | `SuperAdmin` |

**Flujo:** `ISaasTenantAdminService.GetTenantsAsync()` → tabla con nombre, código, plan, estado, fechas clave.

---

### 20.2 Ver Detalle de Tenant

| | |
|---|---|
| **Ruta** | `GET /SuperAdmin/Tenants/Details/{id}` |

**Muestra:** Info del tenant, plan actual, estadísticas de uso (usuarios/doctores/pacientes vs límites), historial de acciones.

---

### 20.3 Crear Tenant

| | |
|---|---|
| **Ruta** | `GET/POST /SuperAdmin/Tenants/Create` |

**Campos:** `Name`, `Code` (slug único), `Email`, `SubscriptionPlanId`, `StartWithTrial`.

**Flujo POST:**
1. `ISaasTenantAdminService.CreateTenantWithSubscriptionAsync(dto, adminUserId)`
2. Crea tenant + suscripción + usuario admin inicial
3. Redirect `Details/{id}`

---

### 20.4 Suspender Tenant

| | |
|---|---|
| **Ruta** | `POST /SuperAdmin/Tenants/Suspend/{id}?reason=` |

**Flujo:** `ISaasTenantAdminService.SuspendTenantAsync(id, reason, superAdminUserId)` → bloquea acceso de todos los usuarios del tenant.

---

### 20.5 Reactivar Tenant

| | |
|---|---|
| **Ruta** | `POST /SuperAdmin/Tenants/Activate/{id}` |

**Flujo:** `ISaasTenantAdminService.ActivateTenantAsync(id, superAdminUserId)`.

---

### 20.6 Cambiar Plan del Tenant

| | |
|---|---|
| **Ruta** | `POST /SuperAdmin/Tenants/ChangePlan/{id}` |

**Flujo:** `ISaasTenantAdminService.ChangePlanAsync(tenantId, newPlanId, superAdminUserId)` → valida límites, actualiza suscripción.

---

## 21. SuperAdmin — Planes y Suscripciones

### 21.1 Listar Planes

| | |
|---|---|
| **Ruta** | `GET /SuperAdmin/Plans` |

**Flujo:** `ISubscriptionPlanAdminService.GetAllAsync()` → tabla con nombre, precio, límites, features incluidas.

---

### 21.2 Listar Suscripciones

| | |
|---|---|
| **Ruta** | `GET /SuperAdmin/Subscriptions` |

**Flujo:** Lista de suscripciones activas/en-prueba con fechas de renovación.

---

### 21.3 Ver Detalle Suscripción

| | |
|---|---|
| **Ruta** | `GET /SuperAdmin/Subscriptions/Details/{id}` |

**Muestra:** Plan, fechas, estado, historial de cambios.

---

## 22. SuperAdmin — Billing SaaS

| | |
|---|---|
| **Ruta** | `GET /SuperAdmin/Billing` |

**Flujo:** `ISaasBillingQueryService.GetBillingMetricsAsync()` → MRR, churn rate, ARR, deuda total, nuevos tenants del período.

---

## 23. Ops / Monitoreo

### 23.1 Dashboard Operacional

| | |
|---|---|
| **Ruta** | `GET /Ops` · `GET /Ops/Home` |
| **Área** | Ops |
| **Rol** | `SuperAdmin` |

**Flujo:**
1. `HealthCheckService.CheckHealthAsync()`
2. `IWorkerHeartbeatService.GetRecentAsync()`
3. Muestra estado de componentes: DB, Cache, servicios externos

---

### 23.2 Health Check Endpoint

| | |
|---|---|
| **Ruta** | `GET /health/startup` |
| **Permiso** | Público (sin auth) |

**Respuesta sanitizada:** `{ "status": "Healthy", "totalDurationMs": 42 }` (sin detalles internos).

---

### 23.3 Estado de Workers / Background Jobs

| | |
|---|---|
| **Ruta** | `GET /Ops/Workers` |

**Flujo:** `IWorkerHeartbeatService.GetRecentAsync()` → tabla con worker ID, último heartbeat, estado.

---

### 23.4 Gestión de Webhooks

| | |
|---|---|
| **Ruta** | `GET /Ops/Webhooks` |

**Flujo:** Lista de webhooks registrados con estado de entregas, retries pendientes.

---

## 24. API Móvil

Todos los endpoints bajo `/api/v1/mobile/` requieren **JWT Bearer** salvo auth endpoints.

### 24.1 Paciente

| Ruta | Método | Descripción |
|---|---|---|
| `/api/v1/mobile/patient` | GET | Datos del paciente autenticado |
| `/api/v1/mobile/patient/allergies` | GET | Alergias del paciente |

### 24.2 Citas

| Ruta | Método | Descripción |
|---|---|---|
| `/api/v1/mobile/appointments` | GET | Lista citas (`from`, `to`, `status`) |
| `/api/v1/mobile/appointments/{id}` | GET | Detalle de cita |

### 24.3 Facturación

| Ruta | Método | Descripción |
|---|---|---|
| `/api/v1/mobile/billing/invoices` | GET | Facturas del paciente |
| `/api/v1/mobile/billing/invoices/{id}` | GET | Detalle de factura |

### 24.4 Notificaciones

| Ruta | Método | Descripción |
|---|---|---|
| `/api/v1/mobile/notifications` | GET | Notificaciones del paciente |
| `/api/v1/mobile/notifications/{id}/read` | POST | Marcar como leída |

### 24.5 Push Notifications

| Ruta | Método | Descripción |
|---|---|---|
| `/api/v1/mobile/push/register` | POST | Registra device token `{ deviceToken, platform }` |
| `/api/v1/mobile/push/unregister` | POST | Elimina device token |

---

## 25. Webhooks e Integraciones

### 25.1 Webhook N8n

| | |
|---|---|
| **Ruta** | `POST /api/webhooks/n8n` |
| **Controlador** | `Api/N8nWebhooksController` |

**Headers requeridos:** `X-N8n-Api-Key` (validación de origen).

**Flujo:** Recibe evento → procesa según tipo → actualiza estado en MedFlow → HTTP 200.

---

### 25.2 Webhook Stripe

| | |
|---|---|
| **Ruta** | `POST /api/webhooks/stripe` |
| **Controlador** | `Api/StripeWebhookController` |

**Eventos procesados:**
- `invoice.paid` → actualiza estado de SaasInvoice
- `invoice.payment_failed` → registra fallo, notifica
- `customer.subscription.updated` → actualiza plan del tenant
- `customer.subscription.deleted` → suspende tenant si no renueva

**Validación:** Verifica firma HMAC del webhook (Stripe signature header).

---

## 26. Mapeo de Permisos

### Tabla completa de permisos (`PermissionCodes.All`)

| Código | Módulo | Acción |
|---|---|---|
| `patients.view` | Pacientes | Ver listado y detalle |
| `patients.create` | Pacientes | Crear paciente |
| `patients.edit` | Pacientes | Editar paciente |
| `patients.delete` | Pacientes | Eliminar paciente |
| `doctors.view` | Doctores | Ver listado y detalle |
| `doctors.create` | Doctores | Crear doctor |
| `doctors.edit` | Doctores | Editar doctor |
| `doctors.delete` | Doctores | Eliminar doctor |
| `appointments.view` | Citas | Ver listado y detalle |
| `appointments.create` | Citas | Crear cita |
| `appointments.edit` | Citas | Editar / cambiar estado de cita |
| `appointments.cancel` | Citas | Cancelar / eliminar cita |
| `medical_records.view` | Expedientes | Ver expediente |
| `medical_records.create` | Expedientes | Crear expediente |
| `medical_records.edit` | Expedientes | Editar expediente |
| `medical_records.delete` | Expedientes | Eliminar expediente |
| `billing.view` | Facturación | Ver facturas y pagos |
| `billing.create` | Facturación | Crear factura |
| `billing.cancel` | Facturación | Anular factura |
| `billing.register_payment` | Facturación | Registrar pago desde factura |
| `billing.manage` | Facturación | Anular pagos, acciones avanzadas |
| `payments.create` | Pagos | Registrar pago independiente |
| `cash.view` | Caja | Ver movimientos |
| `cash.create` | Caja | Registrar movimiento |
| `cash.delete` | Caja | Eliminar movimiento |
| `users.manage` | Usuarios | CRUD de usuarios del tenant |
| `roles.manage` | Roles | CRUD de roles |
| `permissions.view` | Permisos | Ver permisos disponibles |
| `audit.view` | Auditoría | Ver logs de auditoría |
| `dashboard.view` | Dashboard | Ver dashboard ejecutivo |
| `reports.view` | Reportes | Ver reportes y analítica |
| `settings.manage` | Configuración | Modificar configuración del tenant |
| `automations.view` | Automatizaciones | Ver automatizaciones y ejecuciones |
| `automations.manage` | Automatizaciones | Crear/editar/eliminar automatizaciones |
| `event_logs.view` | Event Logs | Ver registro de eventos |
| `ai.insights.view` | IA | Ver insights y copilot |
| `ai.insights.manage` | IA | Aplicar/descartar insights |

---

*Documento generado por análisis directo de código fuente.*
*Total de módulos documentados: 26 · Total de flujos: 98+*
*MedFlow AI — 2026-04-02*
