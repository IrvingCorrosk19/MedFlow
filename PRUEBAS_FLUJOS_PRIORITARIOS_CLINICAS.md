# Plan de pruebas — Flujos prioritarios (clínicas) · MedFlow

**Versión:** 1.0 · **Fecha:** 2026-05-10  
**Objetivo:** Priorizar la validación según el valor para el negocio clínico día a día y reducir riesgo en seguridad, continuidad asistencial y cobro.

---

## 1. Criterio de priorización

Se ordenó por impacto si falla el flujo:

| Nivel | Significado | Enfoque |
|-------|-------------|---------|
| **P0** | Sin esto la clínica no opera el día (agenda, paciente, acceso, aislamiento por tenant) | Smoke diario + antes de release |
| **P1** | Atención clínica y cobro conforme (historia, recetas, factura/pago) | Regresión cada sprint |
| **P2** | Dirección, cumplimiento extendido, paciente en casa | Regresión amplia / staging |
| **P3** | Plataforma SaaS, contabilidad formal, automatizaciones avanzadas | Cuando el módulo cambie |

Los permisos citados corresponden al catálogo en `PermissionCodes` (aplicación).

---

## 2. Flujos prioritarios del dominio clínico

### P0 — Operación diaria

1. **Identidad y sesión** — Login staff, logout, bloqueo por rol/ruta, 2FA si aplica.
2. **Paciente en reception** — Alta rápida, búsqueda, edición mínima; datos disponibles para agenda.
3. **Agenda** — Lista/filtros, crear cita (paciente + doctor + fecha/hora), ver detalle, cambiar estado (confirmada / completada / cancelada / no-show según UI).
4. **Doctor en consulta** — Localizar paciente/cita, abrir expediente o crear nota clínica coherente con la visita.
5. **Portal paciente (opcional por tenant)** — Login paciente, ver próximas citas / información básica sin exponer datos de otros pacientes.

### P1 — Continuidad clínica y tesorería

6. **Historia clínica** — Crear/editar/visualizar según permisos; no filtración entre pacientes.
7. **Recetas** — Emitir y consultar con trazabilidad razonable.
8. **Facturación** — Generar o asociar factura/consumo; registrar pago; saldo coherente.
9. **Caja / movimientos** — Registro acorde a política de la clínica y permisos `cash.*`.

### P2 — Gestión y cumplimiento

10. **Dashboard ejecutivo** — KPIs operativos visibles; bloque financiero solo con `billing.view` (política actual).
11. **Reportes** — Citas, pacientes, doctores según `reports.view`.
12. **Notificaciones / recordatorios** — Recordatorio de cita (jobs), plantillas si están en uso.
13. **Administración** — Usuarios y roles por tenant; ningún rol ve datos fuera de su alcance.

### P3 — Plataforma y finanzas formales

14. **SuperAdmin / tenants** — Alta tenant, límites, facturación SaaS si está activa.
15. **Contabilidad (ledger, períodos, cuentas)** — Solo si el tenant usa el módulo.
16. **Automatizaciones / workflows** — Disparadores tipo `WorkflowTriggerEvents`, ejecuciones y reintentos.

---

## 3. Matriz resumen (qué probar primero)

| ID área | Flujo | Prioridad | Roles típicos |
|---------|-------|-----------|----------------|
| A | Login / logout / 403 esperados | P0 | Todos los QA seed |
| B | Pacientes: listado, crear, editar | P0 | Reception, Staff, Admin |
| C | Citas: listado, crear, editar estado/cancelar | P0 | Reception, Doctor (lectura/agenda según rol) |
| D | Expediente médico: búsqueda, crear/editar | P1 | Doctor |
| E | Recetas | P1 | Doctor |
| F | Facturas + pagos + listados | P1 | Billing |
| G | Dashboard + CSV (operativo vs financiero) | P2 | Admin vs Reception |
| H | Portal paciente | P2 | Patient |
| I | Admin usuarios/roles | P2 | Admin |
| J | SuperAdmin tenants | P3 | SuperAdmin |
| K | Contabilidad / automatizaciones | P3 | Según tenant |

---

## 4. Casos de prueba detallados

Convención: **TP-XXX**. Estado: Pendiente / OK / NOK (rellenar en ejecución).

### Bloque A — Autenticación y autorización (P0)

| ID | Caso | Precondición | Pasos | Resultado esperado |
|----|------|--------------|-------|---------------------|
| TP-A01 | Login válido staff | Usuario QA activo | Abrir `/Account/Login`, credenciales correctas | Redirección a `/` o última URL; UI autenticada |
| TP-A02 | Login rechazado | Contraseña incorrecta | Enviar formulario | Mensaje error; sin sesión |
| TP-A03 | Logout | Sesión activa | Logout desde menú | Login anónimo; rutas protegidas redirigen |
| TP-A04 | Aislamiento rol Reception | Sesión `qa.reception` | Navegar a `AdminUsers` o similar prohibido | 403 o redirect a AccessDenied |
| TP-A05 | Aislamiento Doctor | Sesión `qa.doctor` | Navegar a `BillingInvoices` | 403 |
| TP-A06 | Portal paciente separado | Usuario paciente seed | `/PatientPortal/login` → flujo citado en QA | Inicio paciente sin acceso a staff |

### Bloque B — Pacientes (P0)

| ID | Caso | Precondición | Pasos | Resultado esperado |
|----|------|--------------|-------|---------------------|
| TP-B01 | Listado | Permiso `patients.view` | `Patients/Index` | Tabla carga; filtros sin error |
| TP-B02 | Crear paciente | Permiso `patients.create` | Crear con datos mínimos válidos | Guardado; aparece en listado |
| TP-B03 | Editar paciente | Paciente existente; `patients.edit` | Actualizar teléfono u observación | Cambios persistidos |
| TP-B04 | Detalle paciente | Paciente existente | Abrir `Details` | Datos coherentes; sin error 500 |

### Bloque C — Citas (P0)

| ID | Caso | Precondición | Pasos | Resultado esperado |
|----|------|--------------|-------|---------------------|
| TP-C01 | Listado citas | Permiso `appointments.view` | `Appointments/Index` | Listado con datos seed o vacío estable |
| TP-C02 | Nueva cita | Paciente y doctor existentes; `appointments.create` | Crear cita futura sin solapamiento evidente | Cita creada; visible en listado |
| TP-C03 | Conflicto / validación | Dos citas mismo doctor y franja | Intentar solapar si la UI/servicio lo permiten | Mensaje claro o prevención |
| TP-C04 | Cancelar / cambiar estado | Cita editable; permisos | Cancelar o marcar completada según botones | Estado actualizado en BD/UI |

### Bloque D — Expediente médico (P1)

| ID | Caso | Precondición | Pasos | Resultado esperado |
|----|------|--------------|-------|---------------------|
| TP-D01 | Búsqueda expediente | Sesión doctor | `MedicalRecords/Search` | Resultados solo del tenant |
| TP-D02 | Crear nota / expediente | Paciente atendible | Crear registro con texto diagnóstico/evolución | Persistencia y vínculo paciente correcto |
| TP-D03 | Edición restringida | Usuario sin `medical_records.edit` | Intentar editar vía URL si aplica | Denegación |

### Bloque E — Recetas (P1)

| ID | Caso | Precondición | Pasos | Resultado esperado |
|----|------|--------------|-------|---------------------|
| TP-E01 | Listado / crear | Doctor con permisos | Abrir `Prescriptions`; crear borrador/envío según flujo UI | Sin 500; documento generado o guardado |
| TP-E02 | PDF / impresión | Si existe acción imprimir | Generar salida | Archivo o vista sin datos de otro paciente |

### Bloque F — Facturación y cobro (P1)

| ID | Caso | Precondición | Pasos | Resultado esperado |
|----|------|--------------|-------|---------------------|
| TP-F01 | Listado facturas | `billing.view` | `BillingInvoices/Index` | DataTables OK; importes coherentes |
| TP-F02 | Registrar pago | Factura pendiente; `billing.register_payment` | Registrar pago parcial/total | Saldo actualizado |
| TP-F03 | Movimientos de caja | Permisos cash | `CashMovements` según política | Movimiento registrado y listado |

### Bloque G — Dashboard y exportación (P2)

| ID | Caso | Precondición | Pasos | Resultado esperado |
|----|------|--------------|-------|---------------------|
| TP-G01 | Dashboard operativo | Usuario sin `billing.view` | `/` | KPIs clínicos/agenda visibles; **sin** tarjetas/gráficos financieros |
| TP-G02 | Dashboard financiero | Usuario con `billing.view` | `/` | Bloques financieros visibles |
| TP-G03 | CSV dashboard | Mismo usuario | `Dashboard/ExportCsv` | CSV sin secciones financieras si no hay permiso; con ellas si hay permiso |

### Bloque H — Portal paciente (P2)

| ID | Caso | Precondición | Pasos | Resultado esperado |
|----|------|--------------|-------|---------------------|
| TP-H01 | Login paciente | Cuenta paciente seed | Login portal | Acceso a `/PatientPortal/inicio` |
| TP-H02 | Mis citas / facturas portal | Según menú | Navegar enlaces visibles | Solo datos del paciente autenticado |

### Bloque I — Administración tenant (P2)

| ID | Caso | Precondición | Pasos | Resultado esperado |
|----|------|--------------|-------|---------------------|
| TP-I01 | Listado usuarios | Admin tenant | `AdminUsers` | Tabla y acciones acorde a rol |
| TP-I02 | Roles / permisos | Admin | `AdminRoles` si aplica | Cambios acotados al tenant |

### Bloque J — SuperAdmin (P3)

| ID | Caso | Precondición | Pasos | Resultado esperado |
|----|------|--------------|-------|---------------------|
| TP-J01 | Tenants | SuperAdmin | `SuperAdmin/Tenants` | Listado cross-tenant solo este rol |
| TP-J02 | Admin no SuperAdmin | `qa.admin` | Intentar `SuperAdmin/Tenants` | 403 |

### Bloque K — Contabilidad y automatización (P3)

| ID | Caso | Precondición | Pasos | Resultado esperado |
|----|------|--------------|-------|---------------------|
| TP-K01 | Plan de cuentas / asientos | Tenant con módulo activo | Flujo mínimo lectura | Sin exponer otros tenants |
| TP-K02 | Workflows | Plan con automatización | Listado en `Automations`; ejecución en `WorkflowExecutions` | Estados coherentes |

---

## 5. Datos y entorno sugeridos

- **Usuarios QA:** ver `QA_RESULTADOS_COMPLETOS.md` (`qa.*@medflow.local`, contraseña documentada).
- **Base:** PostgreSQL de desarrollo; tenant demo seed si aplica.
- **Regresión automatizable:** subconjunto P0 (TP-A01–A04, TP-B01–B02, TP-C01–C02) como smoke nightly.

---

## 6. Relación con el análisis de faltantes

Los huecos conocidos por módulo (calendario visual de citas, exportaciones masivas, permisos GET en algunos controladores, etc.) están descritos en `ANALISIS_FALTANTES_MODULO_A_MODULO.md`. Este plan **no** asume que esos ítems estén corregidos: si un caso falla, contrastar con ese documento para clasificar si es **bug** o **funcionalidad pendiente**.

---

## 7. Checklist de cierre de release (mínimo)

- [x] Casos **P0** cubiertos por script HTTP + unit tests (ver §8); manual solo donde se indica  
- [ ] Muestra **P1** extendida (pago PDF, expediente largo) en tenant piloto cuando aplique  
- [x] Verificación **dashboard financiero** por rol (script: TP-G01 / TP-G02 / TP-G03)  
- [x] Portal paciente: login + citas + facturas (TP-H01, TP-H02)  

---

*Documento generado para guiar pruebas manuales y automatizables; ampliar IDs TP según necesidad del equipo QA.*

---

## 8. Ejecución automática (HTTP) — actualizado 2026-05-11

Script: `scripts/ejecutar-pruebas-flujos-prioritarios.ps1`. Valida rutas con sesión real (cookies + antiforgery en POST). **Requisitos:** app en ejecución (`dotnet run --urls http://localhost:5115`), PostgreSQL con migraciones aplicadas.

| Resultado | Detalle |
|-----------|---------|
| **37 / 37 OK** | Incluye mandato v2 (TP-V1…V10), salida ≠ 0 si algún caso es NOK |

**IDs ejecutados por HTTP**

| Bloque | IDs |
|--------|-----|
| A | TP-A02, TP-A01, TP-A03 (logout POST), TP-A04, TP-A05, **TP-A06** |
| B | TP-B01, TP-B02-GET, **TP-B02-POST**, TP-B04, TP-B01-staff |
| C | TP-C01 |
| D | TP-D01 |
| E | TP-E01-GET |
| F | TP-F01, TP-F03 |
| G | TP-G01 (sin KPIs financieros en cuerpo; no confundir con sidebar «Facturación y caja»), TP-G02, TP-G03-admin, TP-G03-reception |
| H | TP-H01, **TP-H02** |
| I | TP-I01, **TP-I02** |
| J | TP-J01, TP-J02 |
| K | TP-K01, TP-K02 |
| **V2 (producto)** | **TP-V1** Experience, **TP-V2** KpiSnapshot, **TP-V3** Growth Engine, **TP-V4** Revenue recovery, **TP-V5** CRM segmentos, **TP-V6** `/portal/dashboard`, **TP-V8** Clinic console, **TP-V9** Security posture, **TP-V10** manifest PWA |

**Notas:** TP-G01 usa negación de etiquetas de tarjetas financieras (`Facturación hoy`, etc.), no la palabra suelta «Facturación» en menú. TP-V10 decodifica `Content` si viene como `byte[]` (`.webmanifest`).

**Correcciones históricas** (referencia): login paciente en `/Account/Login`; migración columnas `Prescriptions`; variable PowerShell `$PID` reservada → uso de `$patientId` en el script.

**Aún manual o ampliación futura (POST multi-paso / PDF):** TP-B03 (editar paciente), TP-C02–C04 (cita crear/solape/estado), TP-D02–D03, TP-E02, TP-F02 (pago), validaciones PDF/export.

**Unit tests:** `dotnet test` — suite completa **197** OK; filtro `PriorityClinicalFlowFunctionalTests` — **4** OK. Informe HTML: `scripts\generate-priority-tests-report.ps1` → `wwwroot/qa/priority-functional-tests-report.html`.

Comando:

```powershell
cd C:\Proyectos\MedFlow
dotnet ef database update --project src\MedFlow.Infrastructure --startup-project src\MedFlow.Web
dotnet run --project src\MedFlow.Web --urls "http://localhost:5115"
# otra terminal:
powershell -ExecutionPolicy Bypass -File .\scripts\ejecutar-pruebas-flujos-prioritarios.ps1 -BaseUrl "http://localhost:5115"
dotnet test tests\MedFlow.UnitTests\MedFlow.UnitTests.csproj -c Release
```
