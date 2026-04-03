# PLAN DE PRUEBAS FUNCIONALES POR ROL — MedFlow AI

> Documento de pruebas funcionales basado en flujos reales del sistema.
> Cada prueba verifica un flujo completo de principio a fin.
> Fecha: 2026-04-03

---

## CREDENCIALES GENERALES

| Rol | Usuario | Contraseña | URL Login |
|---|---|---|---|
| SuperAdmin | `superadmin@medflow.ai` | `MedFlow2026!` | `/Account/Login` |
| Admin | `admin@medflow.ai` | `MedFlow2026!` | `/Account/Login` |
| Doctor | `doctor@medflow.ai` | `MedFlow2026!` | `/Account/Login` |
| Reception | `reception@medflow.ai` | `MedFlow2026!` | `/Account/Login` |
| Billing | `billing@medflow.ai` | `MedFlow2026!` | `/Account/Login` |
| Staff | `staff@medflow.ai` | `MedFlow2026!` | `/Account/Login` |
| Patient | `patient@medflow.ai` | `MedFlow2026!` | `/PatientPortal/Auth/Login` |

---

---

# ROL 1 — SUPERADMIN

**Usuario:** `superadmin@medflow.ai` | **Contraseña:** `MedFlow2026!`
**URL:** `https://localhost:7291/Account/Login`

> El SuperAdmin tiene acceso completo a todos los módulos del sistema incluyendo gestión de tenants, planes y monitoreo operacional.

---

## BLOQUE 1 — Autenticación

### PT-SA-001 · Login exitoso
**Pasos:**
1. Ir a `https://localhost:7291/Account/Login`
2. Ingresar Email: `superadmin@medflow.ai`
3. Ingresar Contraseña: `MedFlow2026!`
4. Click en **Iniciar sesión**

**Resultado esperado:** Redirige a `/Dashboard` y muestra el dashboard ejecutivo con KPIs.

---

### PT-SA-002 · Login con credenciales incorrectas
**Pasos:**
1. Ir a `/Account/Login`
2. Email: `superadmin@medflow.ai` | Contraseña: `incorrecta`
3. Click en **Iniciar sesión**

**Resultado esperado:** Muestra mensaje de error "Correo o contraseña incorrectos." La página NO redirige.

---

### PT-SA-003 · Recuperación de contraseña
**Pasos:**
1. Ir a `/Account/Login`
2. Click en **¿Olvidaste tu contraseña?**
3. Ingresar `superadmin@medflow.ai`
4. Click en **Enviar enlace**
5. Verificar que muestra pantalla de confirmación

**Resultado esperado:** Pantalla de confirmación visible. En desarrollo, el enlace de reset aparece en `TempData["ResetLink"]` o en los logs de la consola.

---

### PT-SA-004 · Logout
**Pasos:**
1. Iniciar sesión como SuperAdmin
2. Click en el menú de usuario (esquina superior derecha)
3. Click en **Cerrar sesión**

**Resultado esperado:** Redirige a `/Account/Login`. Al intentar acceder a `/Dashboard` sin sesión, redirige al login.

---

## BLOQUE 2 — Dashboard y Analítica

### PT-SA-005 · Ver Dashboard Ejecutivo
**Pasos:**
1. Iniciar sesión → ir a `/Dashboard`
2. Verificar que cargan las tarjetas de KPIs (citas hoy, pacientes, facturación, doctores activos)
3. Verificar que los gráficos Chart.js se renderizan

**Resultado esperado:** Dashboard completo con KPIs numéricos, gráficos de barras/líneas y alertas ejecutivas.

---

### PT-SA-006 · Ver Analytics con filtro de rango
**Pasos:**
1. Ir a `/Analytics`
2. Cambiar el filtro de `Desde` y `Hasta` a los últimos 7 días
3. Click en **Aplicar**

**Resultado esperado:** Gráficos actualizados con los datos del rango seleccionado.

---

### PT-SA-007 · Analytics — Agregar snapshot manual
**Pasos:**
1. Ir a `/Analytics`
2. Click en **Agregar ayer** (botón de aggregation)
3. Verificar TempData de éxito

**Resultado esperado:** Mensaje de éxito "Snapshot agregado para [fecha]".

---

### PT-SA-008 · Analytics — Tendencias
**Pasos:**
1. Ir a `/Analytics/Trends`
2. Verificar que cargan los gráficos de tendencias históricas

**Resultado esperado:** Vista con gráficos de tendencias (citas, ingresos, nuevos pacientes) sin errores 500.

---

### PT-SA-009 · Analytics — Benchmarking
**Pasos:**
1. Ir a `/Analytics/Benchmarking`

**Resultado esperado:** Vista de benchmarking cargada sin errores.

---

### PT-SA-010 · Analytics — Snapshots
**Pasos:**
1. Ir a `/Analytics/Snapshots`

**Resultado esperado:** Lista de snapshots diarios cargada.

---

## BLOQUE 3 — Pacientes

### PT-SA-011 · Listar pacientes
**Pasos:**
1. Ir a `/Patients`
2. Verificar que la tabla DataTable carga con datos

**Resultado esperado:** Tabla de pacientes con columnas: Nombre, Teléfono, Correo, Estado, Acciones.

---

### PT-SA-012 · Crear paciente nuevo
**Pasos:**
1. Ir a `/Patients/Create`
2. Completar:
   - Nombre: `Juan`
   - Apellido: `Prueba`
   - Fecha nacimiento: `15/06/1990`
   - Sexo: Masculino
   - Teléfono: `+50766778899`
   - Correo: `juan.prueba@test.com`
3. Click en **Guardar**

**Resultado esperado:** Redirige a `/Patients` con mensaje de éxito "Paciente creado correctamente".

---

### PT-SA-013 · Editar paciente
**Pasos:**
1. Ir a `/Patients`
2. Click en **Editar** sobre el paciente "Juan Prueba"
3. Cambiar teléfono a `+50799887766`
4. Click en **Guardar**

**Resultado esperado:** Redirige con mensaje de éxito y el teléfono actualizado en el detalle.

---

### PT-SA-014 · Ver detalle de paciente
**Pasos:**
1. Ir a `/Patients/Details/{id}` del paciente creado

**Resultado esperado:** Página de detalle con datos clínicos, estado del portal, accesos rápidos a citas, expedientes y facturas.

---

### PT-SA-015 · Habilitar portal del paciente
**Pasos:**
1. Ir a `/Patients/Details/{id}`
2. Click en **Habilitar Portal**
3. Confirmar la operación

**Resultado esperado:** Mensaje de éxito con contraseña temporal para el paciente.

---

### PT-SA-016 · Exportar pacientes a CSV
**Pasos:**
1. Ir a `/Patients`
2. Click en **Exportar CSV**

**Resultado esperado:** Descarga del archivo `pacientes_{fecha}.csv` con los datos de la tabla.

---

### PT-SA-017 · Eliminar paciente (validación de dependencias)
**Pasos:**
1. Crear un paciente de prueba sin citas ni facturas
2. Ir a su listado → Click en **Eliminar**
3. Confirmar la eliminación

**Resultado esperado:** Paciente eliminado y redirige a `/Patients` con mensaje de éxito.

---

## BLOQUE 4 — Doctores

### PT-SA-018 · Crear doctor
**Pasos:**
1. Ir a `/Doctors/Create`
2. Completar:
   - Nombre: `Carlos`
   - Apellido: `Médico`
   - Especialidad: `Medicina General`
   - Teléfono: `+50766001122`
   - Correo: `carlos.medico@clinica.com`
3. Click en **Guardar**

**Resultado esperado:** Redirige al listado con mensaje de éxito.

---

### PT-SA-019 · Filtrar doctores por especialidad
**Pasos:**
1. Ir a `/Doctors`
2. Seleccionar una especialidad en el filtro
3. Click en **Filtrar**

**Resultado esperado:** Solo aparecen los doctores de la especialidad seleccionada.

---

### PT-SA-020 · Exportar doctores a CSV
**Pasos:**
1. Ir a `/Doctors`
2. Click en **Exportar CSV**

**Resultado esperado:** Archivo CSV descargado con datos de doctores.

---

## BLOQUE 5 — Citas

### PT-SA-021 · Crear cita nueva
**Pasos:**
1. Ir a `/Appointments/Create`
2. Seleccionar Paciente: (cualquier paciente existente)
3. Seleccionar Doctor: (cualquier doctor activo)
4. Fecha: mañana
5. Hora inicio: `09:00` | Hora fin: `09:30`
6. Motivo: `Consulta general`
7. Click en **Guardar**

**Resultado esperado:** Redirige al listado con mensaje de éxito y la cita aparece en la tabla.

---

### PT-SA-022 · Validación de hora en cita (EndTime <= StartTime)
**Pasos:**
1. Ir a `/Appointments/Create`
2. Hora inicio: `10:00` | Hora fin: `09:00`
3. Click en **Guardar**

**Resultado esperado:** Error de validación "La hora de fin debe ser posterior a la hora de inicio." La cita NO se crea.

---

### PT-SA-023 · Confirmar cita
**Pasos:**
1. Ir a `/Appointments`
2. Click en **Detalles** de una cita en estado "Programada"
3. Click en **Confirmar**

**Resultado esperado:** Estado de la cita cambia a "Confirmada".

---

### PT-SA-024 · Marcar cita como Completada
**Pasos:**
1. En `/Appointments/Details/{id}` de una cita confirmada
2. Click en **Marcar Completada**

**Resultado esperado:** Estado cambia a "Completada".

---

### PT-SA-025 · Registrar No-Show
**Pasos:**
1. En `/Appointments/Details/{id}`
2. Click en **No Show**

**Resultado esperado:** Estado cambia a "No Show".

---

### PT-SA-026 · Cancelar cita
**Pasos:**
1. En `/Appointments`
2. Click en **Cancelar** sobre una cita
3. Confirmar

**Resultado esperado:** Cita cancelada con mensaje de éxito.

---

## BLOQUE 6 — Expedientes Médicos

### PT-SA-027 · Crear expediente médico
**Pasos:**
1. Ir a `/MedicalRecords/Create?patientId={id}`
2. Completar:
   - Doctor: (seleccionar)
   - Fecha: hoy
   - Diagnóstico: `Infección respiratoria leve`
   - Tratamiento: `Reposo y antibiótico`
   - Temperatura: `38.2`
   - Presión: `120/80`
3. Agregar prescripción: `Amoxicilina 500mg / Cada 8h / 7 días`
4. Click en **Guardar**

**Resultado esperado:** Expediente creado y redirige al historial del paciente.

---

### PT-SA-028 · Ver historial del paciente
**Pasos:**
1. Ir a `/MedicalRecords/Patient/{patientId}`

**Resultado esperado:** Lista cronológica de consultas del paciente.

---

### PT-SA-029 · Editar y eliminar fila de prescripción
**Pasos:**
1. Ir a `/MedicalRecords/Create`
2. Agregar 2 filas de prescripción
3. Click en el botón **Eliminar** (ícono de basura) de la primera fila

**Resultado esperado:** La fila se elimina del formulario sin recargar la página.

---

## BLOQUE 7 — Facturación

### PT-SA-030 · Crear factura
**Pasos:**
1. Ir a `/BillingInvoices/Create`
2. Seleccionar Paciente
3. Fecha de emisión: hoy | Fecha de vencimiento: en 30 días
4. Agregar línea: `Consulta General | Cantidad: 1 | Precio: 50.00`
5. Click en **Crear factura**

**Resultado esperado:** Factura creada y redirige al listado con mensaje de éxito.

---

### PT-SA-031 · Validación DueDate < IssueDate
**Pasos:**
1. Ir a `/BillingInvoices/Create`
2. Fecha emisión: hoy | Fecha vencimiento: ayer
3. Click en **Crear factura**

**Resultado esperado:** Error de validación "La fecha de vencimiento no puede ser anterior a la fecha de emisión."

---

### PT-SA-032 · Imprimir factura
**Pasos:**
1. Ir a `/BillingInvoices/Details/{id}`
2. Click en **Imprimir**

**Resultado esperado:** Abre `/BillingInvoices/Print/{id}` — vista limpia sin sidebar, optimizada para impresión/PDF.

---

### PT-SA-033 · Registrar pago desde factura
**Pasos:**
1. Ir a `/BillingInvoices/Details/{id}` de una factura pendiente
2. Click en **Registrar pago**
3. Ingresar monto: `50.00` | Método: Efectivo | Fecha: hoy
4. Click en **Guardar**

**Resultado esperado:** Estado de factura cambia a "Pagada". Saldo pendiente = 0.

---

### PT-SA-034 · Pago parcial
**Pasos:**
1. Crear factura de $100.00
2. Registrar pago de $60.00
3. Verificar estado de la factura

**Resultado esperado:** Estado cambia a "Pago parcial". Saldo pendiente = $40.00.

---

### PT-SA-035 · Anular pago
**Pasos:**
1. En `/BillingInvoices/Details/{id}` con un pago registrado
2. Click en **Anular pago** junto al pago

**Resultado esperado:** Pago anulado y saldo de la factura restaurado.

---

## BLOQUE 8 — Pagos

### PT-SA-036 · Registrar pago independiente
**Pasos:**
1. Ir a `/Payments/Create?billingInvoiceId={id}`
2. Verificar que aparece el saldo pendiente de la factura
3. Completar monto, método y fecha
4. Click en **Guardar**

**Resultado esperado:** Pago registrado y redirige al listado.

---

### PT-SA-037 · Acceder a Payments/Create sin factura
**Pasos:**
1. Ir a `/Payments/Create` (sin query string)

**Resultado esperado:** Redirige a `/Payments` con mensaje de error "Factura no encontrada."

---

## BLOQUE 9 — Movimientos de Caja

### PT-SA-038 · Crear movimiento de caja (Ingreso)
**Pasos:**
1. Ir a `/CashMovements/Create`
2. Tipo: Ingreso | Monto: `200.00` | Descripción: `Consulta privada`
3. Click en **Guardar**

**Resultado esperado:** Movimiento registrado y aparece en el listado con totales actualizados.

---

### PT-SA-039 · Crear movimiento de caja (Egreso)
**Pasos:**
1. Ir a `/CashMovements/Create`
2. Tipo: Egreso | Monto: `50.00` | Descripción: `Material de oficina`
3. Click en **Guardar**

**Resultado esperado:** Egreso registrado. El neto en el listado refleja la diferencia.

---

## BLOQUE 10 — Reportes

### PT-SA-040 · Reporte de Citas
**Pasos:**
1. Ir a `/Reports/Appointments`
2. Establecer rango de fechas válido (`from <= to`)
3. Click en **Aplicar**

**Resultado esperado:** Tabla con citas del período, totales de completadas/canceladas/no-show.

---

### PT-SA-041 · Validación from > to en Reportes
**Pasos:**
1. Ir a `/Reports/Appointments`
2. Ingresar `Desde: 2026-04-10` y `Hasta: 2026-04-01`
3. Click en **Aplicar**

**Resultado esperado:** Alerta JavaScript "La fecha Desde no puede ser posterior a Hasta." El formulario NO se envía.

---

### PT-SA-042 · Reporte Financiero
**Pasos:**
1. Ir a `/Reports/Financial`
2. Aplicar filtro de fechas del mes actual

**Resultado esperado:** Tabla con facturas y pagos, tarjetas de resumen (Total facturado, Cobrado, Pendiente).

---

### PT-SA-043 · Reporte de Pacientes
**Pasos:**
1. Ir a `/Reports/Patients`
2. Aplicar filtro por rango de fechas

**Resultado esperado:** Tabla con pacientes, columna "Nº citas", nuevos en período, top 15 por citas.

---

### PT-SA-044 · Reporte de Doctores
**Pasos:**
1. Ir a `/Reports/Doctors`
2. Aplicar filtro de fechas

**Resultado esperado:** Tabla con doctores, total citas, completadas, canceladas, no-show, promedio.

---

### PT-SA-045 · Exportar Reporte Pacientes CSV
**Pasos:**
1. Ir a `/Reports/Patients`
2. Click en **Exportar CSV**

**Resultado esperado:** Descarga de archivo CSV con datos del reporte.

---

## BLOQUE 11 — Gestión de Usuarios

### PT-SA-046 · Listar usuarios
**Pasos:**
1. Ir a `/AdminUsers`

**Resultado esperado:** Tabla con todos los usuarios del tenant.

---

### PT-SA-047 · Crear usuario nuevo
**Pasos:**
1. Ir a `/AdminUsers/Create`
2. Email: `test.nuevo@medflow.ai`
3. Contraseña: `Test2026!` | Confirmación: `Test2026!`
4. Nombre: `Test` | Apellido: `Usuario`
5. Rol: Reception
6. Click en **Guardar**

**Resultado esperado:** Usuario creado. Aparece en el listado.

---

### PT-SA-048 · Desactivar usuario
**Pasos:**
1. Ir a `/AdminUsers`
2. Click en **Desactivar** sobre el usuario creado en PT-SA-047

**Resultado esperado:** Estado del usuario cambia a "Inactivo".

---

### PT-SA-049 · Desbloquear usuario
**Pasos:**
1. En `/AdminUsers/Details/{id}` de un usuario bloqueado
2. Click en **Desbloquear**

**Resultado esperado:** Estado de bloqueo eliminado. Mensaje de éxito.

---

### PT-SA-050 · Enviar enlace de restablecimiento de contraseña
**Pasos:**
1. En `/AdminUsers/Details/{id}`
2. Click en **Reset contraseña**
3. Confirmar

**Resultado esperado:** Mensaje de éxito y aparece el enlace de reset en la página de detalles.

---

## BLOQUE 12 — Roles y Permisos

### PT-SA-051 · Ver roles
**Pasos:**
1. Ir a `/AdminRoles`

**Resultado esperado:** Lista de roles con nombre, descripción y número de permisos.

---

### PT-SA-052 · Crear nuevo rol
**Pasos:**
1. Ir a `/AdminRoles/Create`
2. Nombre: `Auditor`
3. Descripción: `Acceso de solo lectura a reportes y auditoría`
4. Click en **Guardar**

**Resultado esperado:** Rol creado y aparece en el listado.

---

### PT-SA-053 · Asignar permisos a rol
**Pasos:**
1. En `/AdminRoles` → Click en el rol `Auditor`
2. Click en **Gestionar permisos**
3. Marcar: `reports.view`, `audit.view`, `dashboard.view`
4. Click en **Guardar**

**Resultado esperado:** Permisos actualizados correctamente.

---

### PT-SA-054 · Eliminar rol con usuarios asignados (debe fallar)
**Pasos:**
1. En `/AdminRoles` intentar eliminar el rol `Admin` (tiene usuarios)
2. Confirmar en el modal SweetAlert2

**Resultado esperado:** Error "No se puede eliminar el rol 'Admin': tiene N usuario(s) asignado(s)." El rol NO se elimina.

---

## BLOQUE 13 — Automatizaciones

### PT-SA-055 · Crear automatización (webhook)
**Pasos:**
1. Ir a `/Automations/Create`
2. Nombre: `Alerta cita cancelada`
3. Evento: `AppointmentCancelled`
4. URL Webhook: `https://webhook.site/test`
5. Método: POST
6. Click en **Crear**

**Resultado esperado:** Automatización creada y aparece en el listado.

---

### PT-SA-056 · Activar/Desactivar automatización
**Pasos:**
1. En `/Automations` toggle del switch de la automatización

**Resultado esperado:** Estado cambia entre Activo/Inactivo sin recargar la página.

---

### PT-SA-057 · Probar webhook (AJAX inline)
**Pasos:**
1. En `/Automations` click en el botón **▶ Probar** de una automatización
2. Verificar resultado inline (sin recarga de página)

**Resultado esperado:** Aparece resultado inline con `statusCode`, `durationMs` y si fue exitoso o no.

---

### PT-SA-058 · Ver ejecuciones de workflow con filtros
**Pasos:**
1. Ir a `/WorkflowExecutions`
2. Filtrar por estado `Failed`
3. Click en **Buscar**

**Resultado esperado:** Solo se muestran ejecuciones fallidas.

---

### PT-SA-059 · Descargar log de ejecución
**Pasos:**
1. En `/WorkflowExecutions/Details/{id}`
2. Click en **Descargar log**

**Resultado esperado:** Descarga de archivo `.log` con detalles de la ejecución.

---

## BLOQUE 14 — Módulo IA

### PT-SA-060 · Copilot — Consulta operativa
**Pasos:**
1. Ir a `/AI/Copilot`
2. Escribir: `¿Cuántas citas hay pendientes para hoy?`
3. Click en **Consultar**
4. Verificar que aparece spinner durante la carga
5. Verificar resultado inline con resumen e items accionables

**Resultado esperado:** Respuesta de IA con resumen y lista de entidades relacionadas con links directos.

---

### PT-SA-061 · Insights de IA
**Pasos:**
1. Ir a `/AI/Insights`
2. Verificar que carga lista de insights
3. Click en **Acknowledge** sobre un insight

**Resultado esperado:** Insight marcado como reconocido.

---

## BLOQUE 15 — Plantillas de Notificación

### PT-SA-062 · Crear plantilla Email
**Pasos:**
1. Ir a `/NotificationTemplates/Create`
2. Canal: Email
3. Verificar que solo se muestran campos de email (Asunto, From, Cuerpo)
4. Completar campos requeridos
5. Click en **Crear plantilla**

**Resultado esperado:** Plantilla creada. Los campos de Webhook e InApp estaban ocultos.

---

### PT-SA-063 · Visibilidad condicional por canal
**Pasos:**
1. Ir a `/NotificationTemplates/Create`
2. Cambiar Canal de Email → Webhook
3. Verificar que desaparecen los campos de Email y aparecen los de Webhook

**Resultado esperado:** Campos cambian dinámicamente sin recargar la página.

---

## BLOQUE 16 — Configuración y Auditoría

### PT-SA-064 · Ver Event Logs
**Pasos:**
1. Ir a `/EventLogs`

**Resultado esperado:** Tabla con eventos del sistema (logins, cambios, etc.).

---

### PT-SA-065 · Ver Configuración
**Pasos:**
1. Ir a `/Settings`

**Resultado esperado:** Formulario con configuraciones del tenant (nombre clínica, zona horaria, moneda, etc.).

---

---

# ROL 2 — ADMIN

**Usuario:** `admin@medflow.ai` | **Contraseña:** `MedFlow2026!`
**URL:** `https://localhost:7291/Account/Login`

> El Admin tiene los mismos permisos que SuperAdmin sobre la clínica, pero NO tiene acceso al área SuperAdmin (gestión de tenants, planes, monitoreo ops).

---

### PT-AD-001 · Login y acceso al Dashboard
**Pasos:**
1. Login con `admin@medflow.ai` / `MedFlow2026!`

**Resultado esperado:** Accede a `/Dashboard` con KPIs. El menú NO muestra opciones de SuperAdmin.

---

### PT-AD-002 · Verificar que NO accede a SuperAdmin
**Pasos:**
1. Intentar ir a `/SuperAdmin/Tenants`

**Resultado esperado:** Redirige a `/Account/AccessDenied` (403).

---

### PT-AD-003 · Gestión completa de pacientes
**Pasos:**
1. Crear paciente → Editar paciente → Ver detalle → Habilitar portal
2. Mismos pasos que PT-SA-012 al PT-SA-016

**Resultado esperado:** Todas las operaciones funcionan correctamente.

---

### PT-AD-004 · Gestión completa de citas
**Pasos:**
1. Crear → Confirmar → Completar → Cancelar cita
2. Mismos pasos que PT-SA-021 al PT-SA-026

**Resultado esperado:** Todas las operaciones de citas disponibles.

---

### PT-AD-005 · Gestión de usuarios del tenant
**Pasos:**
1. Ir a `/AdminUsers`
2. Crear un usuario con rol Reception
3. Desactivar ese usuario
4. Enviar enlace de reset de contraseña

**Resultado esperado:** Todas las operaciones de gestión de usuarios funcionan.

---

### PT-AD-006 · Ver e interpretar reportes
**Pasos:**
1. Ir a `/Reports/Appointments`, `/Reports/Financial`, `/Reports/Patients`, `/Reports/Doctors`
2. Aplicar filtros en cada uno

**Resultado esperado:** Los 4 reportes cargan correctamente con datos.

---

### PT-AD-007 · Gestión de automatizaciones
**Pasos:**
1. Ir a `/Automations`
2. Crear, editar, activar/desactivar una automatización

**Resultado esperado:** Todas las operaciones disponibles como en SuperAdmin.

---

### PT-AD-008 · Acceso a IA — Copilot e Insights
**Pasos:**
1. Ir a `/AI/Copilot` → Hacer consulta
2. Ir a `/AI/Insights` → Ver y acknowledge un insight

**Resultado esperado:** Módulo de IA completamente accesible.

---

---

# ROL 3 — DOCTOR

**Usuario:** `doctor@medflow.ai` | **Contraseña:** `MedFlow2026!`
**URL:** `https://localhost:7291/Account/Login`

> El Doctor tiene acceso a pacientes, citas, expedientes médicos y sus propios reportes. NO tiene acceso a facturación, caja, usuarios, roles ni automatizaciones.

---

### PT-DOC-001 · Login y verificación de menú
**Pasos:**
1. Login con `doctor@medflow.ai` / `MedFlow2026!`

**Resultado esperado:** Accede al dashboard. El menú NO muestra Facturación, Caja, Usuarios, Roles, Automatizaciones.

---

### PT-DOC-002 · Ver listado de pacientes
**Pasos:**
1. Ir a `/Patients`

**Resultado esperado:** Tabla de pacientes visible con opciones de crear, editar y ver detalle.

---

### PT-DOC-003 · Crear y editar expediente médico
**Pasos:**
1. Ir a `/Patients` → Click en un paciente → **Ver historial**
2. Click en **Nuevo expediente**
3. Completar diagnóstico: `Hipertensión leve`
4. Agregar prescripción: `Losartán 50mg / Diario / 30 días`
5. Guardar
6. Editar el expediente recién creado

**Resultado esperado:** Expediente creado y editado correctamente.

---

### PT-DOC-004 · Ver y gestionar sus citas
**Pasos:**
1. Ir a `/Appointments`
2. Crear una cita para un paciente
3. Marcar la cita como Completada

**Resultado esperado:** Cita creada y actualizada a estado "Completada".

---

### PT-DOC-005 · Ver reportes disponibles
**Pasos:**
1. Ir a `/Reports/Appointments`
2. Ir a `/Reports/Patients`
3. Ir a `/Reports/Doctors`

**Resultado esperado:** Los 3 reportes cargan correctamente.

---

### PT-DOC-006 · Verificar que NO accede a Facturación
**Pasos:**
1. Intentar ir a `/BillingInvoices`
2. Intentar ir a `/Payments`

**Resultado esperado:** Ambas rutas devuelven 403 / AccessDenied.

---

### PT-DOC-007 · Verificar que NO accede a Usuarios/Roles
**Pasos:**
1. Intentar ir a `/AdminUsers`
2. Intentar ir a `/AdminRoles`

**Resultado esperado:** Ambas rutas devuelven 403.

---

### PT-DOC-008 · Eliminar expediente médico
**Pasos:**
1. Ir al historial de un paciente
2. Click en **Eliminar** sobre un expediente
3. Confirmar

**Resultado esperado:** Expediente eliminado con mensaje de éxito.

---

---

# ROL 4 — RECEPTION

**Usuario:** `reception@medflow.ai` | **Contraseña:** `MedFlow2026!`
**URL:** `https://localhost:7291/Account/Login`

> Recepción puede gestionar pacientes y citas. NO tiene acceso a expedientes médicos, facturación, caja, usuarios, roles, automatizaciones ni IA.

---

### PT-REC-001 · Login y verificación de menú
**Pasos:**
1. Login con `reception@medflow.ai` / `MedFlow2026!`

**Resultado esperado:** Accede al dashboard. El menú muestra solo: Dashboard, Pacientes, Citas, Reportes.

---

### PT-REC-002 · Agendar nueva cita
**Pasos:**
1. Ir a `/Appointments/Create`
2. Seleccionar paciente y doctor disponibles
3. Fecha: mañana | Hora: `10:00–10:30`
4. Motivo: `Primera consulta`
5. Click en **Guardar**

**Resultado esperado:** Cita creada con estado "Programada".

---

### PT-REC-003 · Confirmar una cita desde el listado
**Pasos:**
1. Ir a `/Appointments`
2. Abrir detalle de una cita en estado "Programada"
3. Click en **Confirmar**

**Resultado esperado:** Estado cambia a "Confirmada".

---

### PT-REC-004 · Registrar un paciente nuevo
**Pasos:**
1. Ir a `/Patients/Create`
2. Completar datos básicos del paciente
3. Click en **Guardar**

**Resultado esperado:** Paciente registrado correctamente.

---

### PT-REC-005 · Cancelar una cita
**Pasos:**
1. Ir a `/Appointments`
2. Click en **Cancelar** sobre una cita
3. Confirmar

**Resultado esperado:** Cita cancelada.

---

### PT-REC-006 · Verificar que NO accede a Expedientes Médicos
**Pasos:**
1. Intentar ir a `/MedicalRecords/Patient/{id}`

**Resultado esperado:** 403 AccessDenied.

---

### PT-REC-007 · Verificar que NO accede a Facturación
**Pasos:**
1. Intentar ir a `/BillingInvoices`

**Resultado esperado:** 403 AccessDenied.

---

### PT-REC-008 · Ver reportes disponibles
**Pasos:**
1. Ir a `/Reports/Appointments`
2. Ir a `/Reports/Patients`

**Resultado esperado:** Ambos reportes cargan.

---

---

# ROL 5 — BILLING

**Usuario:** `billing@medflow.ai` | **Contraseña:** `MedFlow2026!`
**URL:** `https://localhost:7291/Account/Login`

> Billing gestiona facturas, pagos y caja. NO tiene acceso a pacientes (escritura), citas, expedientes, usuarios ni roles.

---

### PT-BIL-001 · Login y verificación de menú
**Pasos:**
1. Login con `billing@medflow.ai` / `MedFlow2026!`

**Resultado esperado:** Dashboard visible. Menú muestra: Dashboard, Facturas, Pagos, Caja, Reportes.

---

### PT-BIL-002 · Crear factura completa
**Pasos:**
1. Ir a `/BillingInvoices/Create`
2. Seleccionar paciente
3. Fecha emisión: hoy | Vencimiento: +30 días
4. Agregar 2 conceptos:
   - `Consulta General | 1 | $50.00`
   - `Examen de sangre | 1 | $30.00`
5. Click en **Crear factura**

**Resultado esperado:** Factura creada por $80.00 en estado "Pendiente".

---

### PT-BIL-003 · Registrar pago completo
**Pasos:**
1. Ir a `/BillingInvoices/Details/{id}` de la factura de $80.00
2. Click en **Registrar pago**
3. Monto: `80.00` | Método: Tarjeta | Fecha: hoy
4. Guardar

**Resultado esperado:** Factura pasa a estado "Pagada". Saldo = $0.

---

### PT-BIL-004 · Registrar pago desde módulo de Pagos
**Pasos:**
1. Ir a `/Payments/Create?billingInvoiceId={id}`
2. Verificar que muestra el saldo pendiente
3. Registrar pago parcial

**Resultado esperado:** Pago registrado. Factura pasa a "Pago parcial".

---

### PT-BIL-005 · Anular factura (sin pagos)
**Pasos:**
1. Crear factura nueva sin registrar ningún pago
2. En el detalle click en **Anular factura**
3. Confirmar

**Resultado esperado:** Factura anulada. Estado = "Cancelada".

---

### PT-BIL-006 · Ver e interpretar reporte financiero
**Pasos:**
1. Ir a `/Reports/Financial`
2. Filtrar por el mes actual

**Resultado esperado:** Tabla con facturas y pagos del mes, resumen de totales.

---

### PT-BIL-007 · Movimientos de caja — Ingreso y Egreso
**Pasos:**
1. Ir a `/CashMovements/Create` → Tipo: Ingreso → Monto: $500 → Guardar
2. Ir a `/CashMovements/Create` → Tipo: Egreso → Monto: $100 → Guardar
3. Ir a `/CashMovements` y verificar totales

**Resultado esperado:** Ambos movimientos registrados. Neto visible = $400.

---

### PT-BIL-008 · Verificar que NO puede editar pacientes
**Pasos:**
1. Intentar ir a `/Patients/Create`
2. Intentar ir a `/Patients/Edit/{id}`

**Resultado esperado:** 403 en ambas rutas. Solo puede acceder a listado de pacientes si tiene `patients.view` (no tiene — 403 también).

---

### PT-BIL-009 · Verificar que NO accede a Citas ni Expedientes
**Pasos:**
1. Intentar ir a `/Appointments`
2. Intentar ir a `/MedicalRecords`

**Resultado esperado:** 403 en ambas rutas.

---

---

# ROL 6 — STAFF

**Usuario:** `staff@medflow.ai` | **Contraseña:** `MedFlow2026!`
**URL:** `https://localhost:7291/Account/Login`

> Staff tiene acceso de solo lectura y operaciones básicas de citas. NO puede crear/editar pacientes, acceder a expedientes, facturación, caja, usuarios ni roles.

---

### PT-STF-001 · Login y verificación de menú limitado
**Pasos:**
1. Login con `staff@medflow.ai` / `MedFlow2026!`

**Resultado esperado:** Dashboard visible. Menú muy reducido: Dashboard, Citas, Pacientes (solo ver), Doctores (solo ver).

---

### PT-STF-002 · Ver listado de pacientes (solo lectura)
**Pasos:**
1. Ir a `/Patients`

**Resultado esperado:** Lista visible. Los botones de **Crear**, **Editar** y **Eliminar** NO aparecen o están deshabilitados.

---

### PT-STF-003 · Ver listado de doctores (solo lectura)
**Pasos:**
1. Ir a `/Doctors`

**Resultado esperado:** Lista visible. Sin opciones de crear ni editar.

---

### PT-STF-004 · Crear una cita
**Pasos:**
1. Ir a `/Appointments/Create`
2. Completar datos de la cita
3. Guardar

**Resultado esperado:** Cita creada (Staff sí tiene `appointments.create`).

---

### PT-STF-005 · Cancelar una cita
**Pasos:**
1. Ir a `/Appointments`
2. Cancelar una cita

**Resultado esperado:** Cita cancelada (Staff tiene `appointments.cancel`).

---

### PT-STF-006 · Verificar que NO accede a Facturación
**Pasos:**
1. Intentar ir a `/BillingInvoices`
2. Intentar ir a `/Payments`
3. Intentar ir a `/CashMovements`

**Resultado esperado:** 403 en todas las rutas.

---

### PT-STF-007 · Verificar que NO accede a Expedientes Médicos
**Pasos:**
1. Intentar ir a `/MedicalRecords`

**Resultado esperado:** 403.

---

### PT-STF-008 · Verificar que NO puede crear pacientes
**Pasos:**
1. Intentar ir a `/Patients/Create`

**Resultado esperado:** 403 (solo tiene `patients.view`, no `patients.create`).

---

---

# ROL 7 — PATIENT (Portal del Paciente)

**Usuario:** `patient@medflow.ai` | **Contraseña:** `MedFlow2026!`
**URL:** `https://localhost:7291/PatientPortal/Auth/Login`

> El paciente accede exclusivamente al portal del paciente. NO puede acceder al sistema de staff bajo ninguna circunstancia.

---

### PT-PAT-001 · Login en el Portal del Paciente
**Pasos:**
1. Ir a `https://localhost:7291/PatientPortal/Auth/Login`
2. Email: `patient@medflow.ai` | Contraseña: `MedFlow2026!`
3. Click en **Iniciar sesión**

**Resultado esperado:** Redirige al home del portal `/PatientPortal/inicio`.

---

### PT-PAT-002 · Verificar que NO puede acceder al sistema staff
**Pasos:**
1. Intentar ir a `https://localhost:7291/Account/Login`
2. Ingresar credenciales del paciente
3. Click en **Iniciar sesión**

**Resultado esperado:** Error "Use el portal del paciente para iniciar sesión con esta cuenta." NO se concede acceso al staff area.

---

### PT-PAT-003 · Ver Home del Portal con KPIs reales
**Pasos:**
1. Acceder al portal como paciente
2. Verificar en la pantalla de inicio:
   - Tarjeta de próxima cita (fecha y doctor)
   - Tarjeta de estado de cuenta (saldo pendiente)
   - Tarjeta de notificaciones (conteo real)

**Resultado esperado:** Los 3 widgets muestran datos reales del paciente (no hardcodeados).

---

### PT-PAT-004 · Ver citas próximas
**Pasos:**
1. Ir a `/PatientPortal/citas`

**Resultado esperado:** Lista de citas próximas con fecha, hora, doctor y estado.

---

### PT-PAT-005 · Cancelar una cita con motivo
**Pasos:**
1. En `/PatientPortal/citas` click en **Cancelar** sobre una cita
2. Verificar que se abre un modal con un campo de texto para el motivo
3. Ingresar motivo: `No podré asistir por viaje`
4. Click en **Confirmar cancelación**

**Resultado esperado:** Cita cancelada. Desaparece de la lista de próximas citas.

---

### PT-PAT-006 · Ver historial de citas
**Pasos:**
1. Ir a `/PatientPortal/citas/historial`

**Resultado esperado:** Lista cronológica inversa de citas pasadas.

---

### PT-PAT-007 · Ver detalle de una cita
**Pasos:**
1. En el historial de citas, click en una cita

**Resultado esperado:** Vista detallada con doctor, consultorio, notas y estado.

---

### PT-PAT-008 · Ver y editar perfil
**Pasos:**
1. Ir a `/PatientPortal/perfil`
2. Cambiar número de teléfono
3. Click en **Guardar**

**Resultado esperado:** Datos actualizados correctamente.

---

### PT-PAT-009 · Cambiar contraseña desde el portal
**Pasos:**
1. Ir a `/PatientPortal/perfil/cambiar-password`
2. Contraseña actual: `MedFlow2026!`
3. Nueva contraseña: `NuevaClave2026!`
4. Confirmar: `NuevaClave2026!`
5. Click en **Guardar**

**Resultado esperado:** Contraseña actualizada. Mensaje de éxito. La nueva contraseña funciona para el siguiente login.

---

### PT-PAT-010 · Ver facturas
**Pasos:**
1. Ir a `/PatientPortal/facturas`

**Resultado esperado:** Lista de facturas con estado (Pagada / Pendiente / Vencida). Muestra saldo total pendiente.

---

### PT-PAT-011 · Ver detalle de factura
**Pasos:**
1. Click en una factura en el portal

**Resultado esperado:** Detalle con conceptos, montos y pagos registrados.

---

### PT-PAT-012 · Ver estado de cuenta
**Pasos:**
1. Ir a `/PatientPortal/estado-cuenta`

**Resultado esperado:** Resumen con saldo total pendiente, total pagado e historial de facturas.

---

### PT-PAT-013 · Ver notificaciones
**Pasos:**
1. Ir a `/PatientPortal/notificaciones`

**Resultado esperado:** Lista de notificaciones con fecha y tipo.

---

### PT-PAT-014 · Marcar notificación como leída
**Pasos:**
1. En `/PatientPortal/notificaciones`
2. Click en **Marcar como leída** sobre una notificación

**Resultado esperado:** Notificación marcada. El conteo en el widget de Home se actualiza.

---

### PT-PAT-015 · Logout del portal
**Pasos:**
1. Click en **Cerrar sesión** en el portal del paciente

**Resultado esperado:** Redirige a `/PatientPortal/Auth/Login`. Al intentar acceder al portal sin sesión redirige al login.

---

---

## RESUMEN DE COBERTURA POR ROL

| Rol | Total pruebas | Módulos cubiertos |
|---|---|---|
| SuperAdmin | 65 | Dashboard, Pacientes, Doctores, Citas, Expedientes, Facturación, Pagos, Caja, Reportes, Analytics, Usuarios, Roles, Automatizaciones, IA, Notificaciones, Settings |
| Admin | 8 | Dashboard, Pacientes, Doctores, Citas, Expedientes, Facturación, Pagos, Caja, Reportes, Analytics, Usuarios, Roles, Automatizaciones, IA |
| Doctor | 8 | Dashboard, Pacientes, Citas, Expedientes Médicos, Reportes |
| Reception | 8 | Dashboard, Pacientes, Citas, Reportes |
| Billing | 9 | Dashboard, Facturación, Pagos, Caja, Reportes |
| Staff | 8 | Dashboard, Citas (CRUD), Pacientes (ver), Doctores (ver) |
| Patient | 15 | Portal: Home, Citas, Historial, Perfil, Contraseña, Facturas, Notificaciones |
| **TOTAL** | **121** | **Todos los módulos** |

---

*Documento generado a partir de permisos reales en BD y flujos del código fuente.*
*MedFlow AI — 2026-04-03*
