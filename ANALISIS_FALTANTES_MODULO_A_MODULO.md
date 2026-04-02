# ANÁLISIS DE FALTANTES POR MÓDULO — MedFlow

> Auditoría funcional y técnica. Solo faltantes reales encontrados en código.
> Fecha de análisis: 2026-04-02

---

## 1. Dashboard (Ejecutivo)

### Faltantes funcionales
- El filtro de días está hardcodeado en el controlador como `new ExecutiveDashboardFilter(14)`. No existe un selector de rango de fecha funcional que el usuario pueda cambiar desde la vista.
- Los botones "Excel" y "PDF" en la barra de herramientas están deshabilitados (`disabled`). No existe ninguna ruta ni acción de backend que los maneje.
- El botón de imprimir usa `window.print()` sin hoja de estilos de impresión; los gráficos Chart.js no se renderizan bien al imprimir.
- No hay opción de actualización automática o recarga de datos sin recargar la página completa.
- No existe selector de rango personalizado (`from`/`to`) en la vista del dashboard ejecutivo; solo `days`.

### Faltantes de frontend
- Los 8 canvas de gráficos (`chartAptDay`, `chartAptStatus`, `chartRevenue`, `chartNewPatients`, `chartTopSpec`, `chartTopDoc`, `chartPayMethod`, `chartCancelTrend`) dependen de que `MedFlowExecutiveDashboard.init(p)` en `~/js/dashboard/executive-dashboard.js` cargue correctamente. Si el script falla, los canvas quedan vacíos sin mensaje de error visible al usuario.
- No existe estado vacío visible si el tenant no tiene datos (el try/catch en la vista solo hace `console.error`, no muestra nada al usuario).
- No hay indicador de carga mientras se obtienen los datos del dashboard.
- Los KPI cards muestran números pero no tienen enlace directo al módulo correspondiente (ej. clicking en "Citas hoy" debería filtrar el listado de citas).

### Faltantes de backend
- `DashboardController.Index` no tiene try/catch. Si `_analytics.GetExecutiveDashboardAsync()` lanza excepción, el usuario recibe un error 500 sin manejo.
- No existe validación de que el tenant tiene datos antes de construir el dashboard. Un tenant recién creado ve valores en cero sin ningún mensaje contextual.
- No existe caché de los resultados del dashboard. Cada visita recalcula todo en tiempo real.

### Faltantes de validación
- Sin validación de que el rango de días solicitado sea positivo o razonable (aunque está hardcodeado, si se expone el filtro, será vulnerable a valores negativos o extremadamente grandes).

### Faltantes de integración
- El dashboard ejecutivo no integra los datos de AI Insights aunque el módulo de IA existe. No hay panel de alertas de IA en el dashboard principal.

### Faltantes de UX/UI
- Los "Executive Alerts" (alertas ejecutivas) no tienen acción asociada: se muestran pero no hay botón para navegar al problema o descartarlas.
- "RecentActivity" muestra timestamps pero sin formato relativo (ej. "hace 5 min"). Se muestra la fecha/hora absoluta sin contexto.
- "UpcomingAppointments" no tiene enlace al detalle de cada cita.

### Faltantes de seguridad o control
- No hay verificación de que el usuario tiene acceso al tenant antes de mostrar sus datos (se confía solo en el tenant context del middleware).

### Faltantes para cierre del módulo
- Implementar exportación a Excel/PDF real.
- Añadir selector de rango de fecha dinámico funcional.
- Añadir try/catch en el controlador con vista de error amigable.
- Hoja de estilos de impresión para charts.
- Enlace de navegación desde KPIs hacia módulos correspondientes.

---

## 2. Pacientes

### Faltantes funcionales
- No existe acción de reactivación de paciente desde el listado. Solo desde `Details` está el botón EnablePortal/DisablePortal, que es distinto a activar/desactivar al paciente como registro.
- No existe importación masiva de pacientes (CSV/Excel).
- No existe exportación del listado de pacientes.
- El campo "Observaciones" del paciente se muestra en `Details` pero no existe buscador por ese campo.
- La búsqueda en `Index` solo filtra por nombre y estado activo. No hay filtro por documento, teléfono, doctor tratante, o fecha de registro.

### Faltantes de frontend
- `Details.cshtml`: El botón "Habilitar Portal" está deshabilitado si el paciente no tiene correo (`disabled` y title explicativo), pero el botón persiste visible creando confusión de UX. Debería ocultarse o reemplazarse con un mensaje más claro.
- No existe vista de historial de cambios del paciente (quién modificó qué y cuándo).
- En `Index`, la columna "Teléfono" muestra el dato pero no es un enlace `tel:`. La columna "Correo" no es un enlace `mailto:`.
- No existe vista de paciente con resumen consolidado: citas pendientes, saldo pendiente, último expediente.

### Faltantes de backend
- `PatientsController.Details` (GET) no tiene `[RequirePermission]`. Cualquier usuario autenticado puede ver el detalle de cualquier paciente sin restricción de permiso.
- `PatientsController.Edit` (GET) no tiene `[RequirePermission]`. El formulario de edición es accesible sin verificar permiso `PatientsEdit`.
- `PatientsController.Delete` no tiene try/catch. Si el servicio falla, el usuario recibe un 500 sin mensaje.
- No existe soft-delete confirmado en la vista: se elimina sin verificar si el paciente tiene citas activas, facturas pendientes o expedientes médicos.
- No existe validación de unicidad de número de documento al crear o editar.

### Faltantes de validación
- No hay validación de que el número de documento sea único dentro del tenant antes de guardar.
- No hay validación de formato de teléfono más allá del atributo `[Phone]` básico.
- No hay validación de que la fecha de nacimiento sea razonable (no futura, no más de 150 años atrás).
- No hay validación de que el correo electrónico no esté ya registrado como portal de otro paciente.

### Faltantes de integración
- Al eliminar un paciente no se verifica si tiene citas futuras programadas, expedientes médicos, o facturas con saldo pendiente.
- No existe un evento disparado al habilitar/deshabilitar el portal del paciente (no se registra en EventLog desde el controlador).

### Faltantes de UX/UI
- No existe paginación configurable por el usuario (DataTable sí pagina, pero el servidor devuelve todos los registros siempre).
- No existe estado vacío con call-to-action cuando no hay pacientes registrados.
- No existe feedback de éxito/error inline al habilitar/deshabilitar portal (solo TempData que requiere recarga).

### Faltantes de seguridad o control
- `Details` y `Edit` GET carecen de `[RequirePermission]`, exponiendo datos clínicos sin control granular.
- Al eliminar un paciente no se registra en AuditLog desde el controlador.

### Faltantes para cierre del módulo
- Añadir `[RequirePermission(PatientsView)]` a `Details` GET.
- Añadir `[RequirePermission(PatientsEdit)]` a `Edit` GET.
- Validación de unicidad de documento.
- Verificación de dependencias antes de eliminar.
- Exportación de listado.
- Vista consolidada del paciente.

---

## 3. Doctores

### Faltantes funcionales
- No existe gestión del horario laboral del doctor desde la UI. El modelo `Doctor` tiene campos de horario pero no hay formulario para configurarlos.
- No existe vista del calendario de disponibilidad del doctor.
- No existe carga masiva de doctores.
- No existe exportación del listado de doctores.
- No existe filtro por especialidad en el listado de doctores.

### Faltantes de frontend
- Identical al módulo de Pacientes: `Details` y `Edit` GET no verifican permisos (probable, dado el patrón observado en controladores).
- No existe vista de agenda del doctor: sus citas para hoy/semana.
- No existe sección de métricas del doctor (pacientes atendidos, tasa de cancelación, etc.) aunque el Dashboard ejecutivo sí usa esos datos.

### Faltantes de backend
- El formulario usa `_DoctorForm` partial. Sin leer ese partial es posible que los campos de `ConsultationRoom` y `WorkingHours` estén presentes en el modelo pero no en el formulario.
- No existe acción para desactivar/reactivar doctor.
- No existe validación de unicidad de número de licencia médica.

### Faltantes de validación
- Sin validación de que la licencia médica sea única dentro del tenant.
- Sin validación de formato de correo/teléfono más allá de anotaciones básicas.

### Faltantes de integración
- Al eliminar un doctor no se verifica si tiene citas futuras activas ni expedientes médicos asociados.
- No se integra la gestión de horario del doctor con el módulo de citas para prevenir conflictos de agendamiento.

### Faltantes de UX/UI
- No existe filtro por especialidad en el listado.
- No existe avatar o foto del doctor (el modelo podría soportarlo pero la UI no lo expone).

### Faltantes de seguridad o control
- Al igual que Pacientes, probable ausencia de `[RequirePermission]` en `Details` y `Edit` GET (patrón consistente encontrado).

### Faltantes para cierre del módulo
- Gestión visual de horario/disponibilidad.
- Validación de unicidad de licencia.
- Verificación de dependencias antes de eliminar.
- Filtro por especialidad en listado.
- Vista de agenda individual del doctor.

---

## 4. Citas (Appointments)

### Faltantes funcionales
- No existe vista de calendario (solo tabla). El módulo promete agendamiento pero no hay vista de agenda visual (semana/mes).
- No existe reprogramación de citas: solo editar el registro completo.
- No existe selección de slot disponible: el usuario debe conocer la disponibilidad del doctor de antemano.
- La detección de conflictos (`HasConflictAsync`) existe en el servicio pero no queda claro si el controlador la llama durante Create/Edit y muestra un error adecuado.
- No existe confirmación automática de citas (solo manual).
- No existe vista de citas del día actual filtrada por defecto.

### Faltantes de frontend
- `_AppointmentForm` partial no fue analizado directamente. Basado en el controlador, el partial debe incluir `PatientId`, `DoctorId`, `ScheduledDate`, `StartTime`, `EndTime`, `Reason` pero no está confirmada la presencia de validación de solapamiento visible al usuario.
- El filtro de `Index` tiene `from`, `to`, `doctorId` pero no tiene filtro por estado de cita.
- El DataTable ordena por fecha pero no hay agrupación por día.
- No existe botón de "Marcar como completada" o "Marcar como no-show" desde el listado. Solo se puede hacer desde `Edit`.

### Faltantes de backend
- `AppointmentsController.Details` (GET): sin `[RequirePermission]`.
- `AppointmentsController.Edit` (GET): sin `[RequirePermission]`.
- `AppointmentsController.Delete` usa `[RequirePermission(AppointmentsCancel)]` pero el método se llama `Delete`. El nombre del permiso no coincide con la acción visual.
- No existe try/catch en `Delete`. Si el servicio falla, el usuario recibe 500.
- No existe TempData de error si la eliminación falla.
- `Index` retorna `View()` sin modelo, dependiendo completamente de ViewBag para los datos.

### Faltantes de validación
- No hay validación visible de que `EndTime > StartTime`.
- No hay validación de que la fecha de la cita no sea en el pasado al crear.
- No hay validación de que el doctor está activo.
- No hay validación de que el paciente está activo.

### Faltantes de integración
- Al cancelar una cita no se dispara notificación automática al paciente (depende de si el WorkflowTrigger está configurado manualmente).
- No hay integración visual entre la cita y su expediente médico correspondiente (si existe).
- No hay enlace desde la cita hacia la factura generada por esa cita.

### Faltantes de UX/UI
- Sin vista de calendario, el agendamiento es ciego: el recepcionista no puede ver visualmente si hay conflicto.
- No existe diferenciación de color por estado de cita en la tabla (las badges existen pero la fila completa podría colorear).
- No existe indicador de "cita próxima en X minutos" para las de hoy.
- El campo "Motivo" se trunca en la tabla sin tooltip que muestre el texto completo.

### Faltantes de seguridad o control
- `Details` y `Edit` GET sin `[RequirePermission]`.
- No se registra en AuditLog la cancelación/eliminación de citas.

### Faltantes para cierre del módulo
- Vista de calendario (mínimo semana).
- Validación de conflictos visible al usuario.
- `[RequirePermission]` en `Details` y `Edit` GET.
- Validación de `EndTime > StartTime` y fecha no pasada.
- Botones de acción de estado rápido desde el listado.
- Integración de notificación al cancelar.

---

## 5. Expedientes Médicos (Medical Records)

### Faltantes funcionales
- `MedicalRecordsController.Create` (GET) sin `[RequirePermission]`.
- `MedicalRecordsController.Edit` (GET) sin `[RequirePermission]`.
- `MedicalRecordsController.Delete` (POST) sin `[RequirePermission]`.
- No existe validación de que el expediente no sea duplicado para el mismo paciente en la misma fecha.
- No existe vista de impresión/exportación del expediente médico (historia clínica en PDF).
- Los adjuntos (`MedicalAttachment`) se pueden subir pero no se indica qué tipos MIME son permitidos más allá de la validación de extensión.
- En el formulario de recetas (`rxTable`), no existe botón de eliminar fila. Las líneas de prescripción solo se pueden agregar, no remover, antes de guardar.

### Faltantes de frontend
- El formulario de creación de expediente tiene campos de signos vitales (`HeightCm`, `WeightKg`, `BloodPressure`, `HeartRateBpm`, `TemperatureCelsius`) sin validación de rangos razonables en el frontend.
- No existe indicador de IMC calculado automáticamente al ingresar talla/peso.
- La lista de expedientes del paciente (`Patient.cshtml`) no tiene paginación ni ordenamiento visible.
- No existe búsqueda dentro del expediente del paciente por diagnóstico o fecha.
- No existe vista de comparación de signos vitales entre consultas (tendencia de salud del paciente).
- No existe sección para ver los adjuntos cargados (galería/lista de archivos) desde la vista de detalle del expediente.

### Faltantes de backend
- `UploadAttachment`: extensión validada con array de extensiones permitidas pero sin validación de tipo MIME real (solo extensión del nombre). Un archivo malicioso puede renombrarse.
- `UploadAttachment`: ruta construida con `Path.Combine` pero sin verificar que el directorio de destino existe antes de escribir.
- `UploadAttachment`: no hay límite por archivo individual validado en código (solo el `[RequestSizeLimit]` global de 50MB).
- No existe acción para eliminar un adjunto individual.
- No existe validación de que la prescripción tenga al menos `MedicationName` no vacío antes de guardar.
- No existe try/catch en `Create` POST ni en `Edit` POST.

### Faltantes de validación
- `HeartRateBpm`: sin validación de rango (ej. 30–300).
- `BloodPressure`: es texto libre. No hay validación de formato `120/80`.
- `TemperatureCelsius`: sin validación de rango razonable (ej. 30.0–45.0).
- `WeightKg` y `HeightCm`: sin validación de rangos mínimos/máximos.
- Sin validación de que `DoctorId` corresponda a un doctor activo del tenant.

### Faltantes de integración
- La vinculación `AppointmentId` en el expediente existe pero no se verifica que la cita pertenezca al mismo paciente.
- No existe generación automática de expediente al completar una cita.
- No hay integración con el módulo de facturación para asociar consulta → factura desde el expediente.

### Faltantes de UX/UI
- La tabla de prescripciones en el formulario no tiene botón "Eliminar fila" (solo "Agregar").
- La vista de detalle del expediente no muestra los adjuntos cargados como lista navegable.
- No existe estado vacío diferenciado en `MedicalRecords/Index` cuando hay pacientes sin expedientes vs. cuando no hay resultados de búsqueda.

### Faltantes de seguridad o control
- Sin `[RequirePermission]` en `Create` GET, `Edit` GET y `Delete` POST.
- Upload de archivos sin validación MIME real.
- No se registra en AuditLog la creación/edición/eliminación de expedientes.

### Faltantes para cierre del módulo
- `[RequirePermission]` en las 3 acciones que carecen de él.
- Botón "Eliminar fila" en tabla de prescripciones.
- Validación de rango en signos vitales (frontend + backend).
- Validación MIME real en uploads.
- Exportación a PDF del expediente.
- Vista/lista de adjuntos en el detalle.
- Try/catch en Create y Edit POST.

---

## 6. Facturación — Facturas (BillingInvoices)

### Faltantes funcionales
- `BillingInvoicesController.Create` (POST) no tiene `[RequirePermission]`. Cualquier usuario autenticado puede crear facturas.
- `BillingInvoicesController.CancelPayment` (POST) no tiene `[RequirePermission]`. Anular pagos es una acción financiera crítica sin control de permiso.
- No existe acción de editar una factura ya creada (solo crear y cancelar).
- No existe generación automática de factura desde una cita completada.
- No existe vista de factura en formato imprimible/PDF.
- El número de factura se genera en el servicio sin validación de unicidad expuesta al usuario si falla.

### Faltantes de frontend
- `Create.cshtml`: Las líneas de conceptos (`linesTable`) no validan que `UnitPrice > 0` ni que `Quantity > 0` en el cliente antes de enviar.
- `Create.cshtml`: El campo `AppointmentId` es `readonly` pero se envía como hidden input. Si hay manipulación del DOM, puede enviarse cualquier valor.
- `Create.cshtml`: No hay validación visual de que `DueDate >= IssueDate`.
- `Details.cshtml`: El botón "Anular factura" solo aparece si `AmountPaid == 0`, pero si hay pagos parciales y el usuario quiere cancelar el saldo, no hay flujo para ello.
- `Index.cshtml`: La acción por fila solo tiene "Ver Detalle". No hay acciones rápidas de registrar pago desde el listado.

### Faltantes de backend
- Sin try/catch en `Create` POST.
- `User.FindFirst(ClaimTypes.NameIdentifier)` en `RegisterPayment` y `CancelPayment` no está null-guarded. Si el claim no existe, lanzará NullReferenceException.
- No existe validación de que `DueDate >= IssueDate`.
- No existe validación de que las líneas de conceptos tengan cantidad y precio positivos.
- `CancelPayment` (POST) sin `[RequirePermission]`.

### Faltantes de validación
- Sin validación de que el total de la factura sea mayor a 0 antes de guardar.
- Sin validación de que el `DiscountAmount` no sea mayor al subtotal.
- Sin validación de formato de `IssueDate` (podría enviarse fecha futura).

### Faltantes de integración
- No existe enlace automático entre la factura y la cita correspondiente en el flujo de creación (se puede ingresar `AppointmentId` opcionalmente pero no es guiado).
- El pago registrado desde `Details` y el pago registrado desde `Payments/Create` son flujos duplicados que deben sincronizarse.

### Faltantes de UX/UI
- No existe vista imprimible de la factura.
- No existe exportación a PDF.
- No existe envío de factura por correo al paciente.
- El estado "PartiallyPaid" (Pago parcial) no muestra visualmente cuánto se ha pagado y cuánto falta directamente en el listado.
- No hay indicador visual de facturas vencidas (overdue).

### Faltantes de seguridad o control
- `Create` POST y `CancelPayment` POST sin `[RequirePermission]`.
- `User.FindFirst()` sin null check en acciones financieras.

### Faltantes para cierre del módulo
- `[RequirePermission]` en `Create` POST y `CancelPayment` POST.
- Null check en `User.FindFirst()`.
- Validación de `DueDate >= IssueDate`.
- Validación de líneas positivas.
- Vista de impresión/PDF.
- Indicador de facturas vencidas en el listado.

---

## 7. Pagos (Payments)

### Faltantes funcionales
- `PaymentsController.Create` (POST) sin `[RequirePermission]`. Registrar pagos sin control de permiso.
- No existe acción para anular un pago desde `Payments/Index` o `Payments/Details`. Solo desde `BillingInvoices/Details`.
- No existe exportación del listado de pagos (similar al problema en Facturas).
- No existe conciliación de pagos (comparar lo esperado vs. lo recibido).

### Faltantes de frontend
- `Create.cshtml`: Si se navega a `Payments/Create` sin un `BillingInvoiceId` en query string, el formulario se muestra vacío sin mensaje de error. El usuario puede intentar guardar un pago sin factura asociada.
- `Index.cshtml`: La acción por fila solo tiene "Ver Detalle". No hay anulación rápida desde el listado.
- El campo `Amount` en `Create` no valida visualmente que el monto no supere el saldo pendiente de la factura.

### Faltantes de backend
- `PaymentsController.Create` (POST) sin `[RequirePermission]`.
- `User.FindFirst(ClaimTypes.NameIdentifier)` en `Create` POST sin null check.
- No existe validación de que el `Amount` no supere el saldo pendiente de la factura.
- No existe protección contra pagos duplicados (mismo monto, misma fecha, misma factura).
- `Create` GET: Si la factura no existe (líneas 64-70), retorna el formulario vacío en lugar de redirigir con mensaje de error.

### Faltantes de validación
- Sin validación de que `Amount > 0`.
- Sin validación de que `PaymentDate` no sea futura de manera irrazonable.
- Sin validación de que `Amount <= BalanceDue`.

### Faltantes de integración
- Después de registrar un pago, no se verifica si la factura queda completamente saldada para actualizar su estado automáticamente (esto podría estar en el servicio, pero no está confirmado en el controlador).

### Faltantes de UX/UI
- Sin mensaje de error cuando se accede a `Create` sin factura.
- Sin indicador del saldo pendiente de la factura en el formulario de pago (el usuario no sabe cuánto puede pagar).
- Sin historial de intentos de pago fallidos.

### Faltantes de seguridad o control
- `Create` POST sin `[RequirePermission]`.
- Sin null check en `User.FindFirst()`.

### Faltantes para cierre del módulo
- `[RequirePermission]` en `Create` POST.
- Validación de `Amount <= BalanceDue`.
- Protección anti-duplicado.
- Mensaje de error al acceder sin factura.
- Indicador de saldo pendiente en formulario de creación.

---

## 8. Movimientos de Caja (CashMovements)

### Faltantes funcionales
- La vista `Index` no fue analizada en detalle pero el controlador solo tiene `Index`. No existe Create, Edit ni Delete.
- No existe forma de registrar movimientos manuales de caja desde la UI (solo lectura de lo que ya existe).
- No existe exportación del flujo de caja.
- No existe filtro por tipo de movimiento o rango de fechas visible en la vista.

### Faltantes de frontend
- Vista limitada a lectura, sin acciones.

### Faltantes de backend
- Sin acciones de escritura en el controlador. El módulo es read-only pero el modelo `CashMovement` existe como entidad completa.

### Faltantes de UX/UI
- Sin totales acumulados por día/semana/mes.
- Sin gráfico de flujo de caja en la vista.

### Faltantes para cierre del módulo
- Definir si movimientos de caja son solo automáticos (generados por pagos) o si deben poder registrarse manualmente.
- Si es manual: implementar Create/Edit/Delete.
- Agregar filtros y totales en la vista.

---

## 9. Reportes (Reports)

### Faltantes funcionales
- El botón "Exportar Excel" en el reporte de citas está deshabilitado con tooltip "Próximamente". No existe ningún endpoint ni implementación de exportación.
- No existe exportación en ninguno de los reportes (citas, financiero, pacientes, doctores).
- Los reportes de Pacientes y Doctores existen en el controlador pero sus vistas no fueron confirmadas como completamente implementadas.
- No existe reporte de Expedientes Médicos.
- No existe reporte de No-shows o tasa de cancelación como reporte independiente (solo en Analytics).

### Faltantes de frontend
- El listado financiero usa `Take(500)` para la lista desplegable de pacientes. Si el tenant tiene más de 500 pacientes, el filtro queda truncado sin advertencia.
- Las fechas en reportes están formateadas con formato hardcodeado (`dd/MM/yyyy` en Citas, `g` en Financiero). No respetan la configuración de fecha del tenant definida en Onboarding.
- Los montos en el reporte financiero no muestran símbolo de moneda ni código de divisa. Solo el número con 2 decimales. No respetan la configuración de moneda del tenant.
- El reporte de Citas no tiene columna de "Sala/Consultorio".
- El reporte financiero no tiene subtotales por método de pago visibles como columna (solo como tarjetas de resumen).

### Faltantes de backend
- `ReportsController`: La obtención de especialidades para el filtro consulta todos los doctores y los proyecta en memoria (`Distinct()`). Para tenants grandes, esto es ineficiente.
- No existe `AnalyticsExportController` funcionalidad confirmada para exportar datos de reportes.
- No existe caché de resultados de reportes.

### Faltantes de validación
- Sin validación de que `from <= to` en los filtros de fecha de todos los reportes.
- Sin validación de rangos máximos (ej. no se puede pedir reporte de 5 años sin paginación).

### Faltantes de integración
- No existe enlace desde los reportes hacia los registros individuales (ej. click en una cita del reporte no navega al detalle de la cita).

### Faltantes de UX/UI
- Sin exportación a ningún formato.
- Sin opción de guardar configuración de reporte como favorito.
- Sin totales de fila visibles en el DataTable (solo server-side totals en tarjetas).
- Sin indicador de "N resultados encontrados" antes de la tabla.

### Faltantes para cierre del módulo
- Implementar exportación a Excel/CSV.
- Respetar formato de fecha y moneda del tenant.
- Agregar validación `from <= to`.
- Completar vistas de reportes de Pacientes y Doctores.
- Enlace desde registros del reporte hacia su detalle.

---

## 10. Analítica (Analytics)

### Faltantes funcionales
- `AnalyticsController.Trends` (GET) sin `[RequirePermission]`. Datos sensibles de tendencias accesibles sin control de permiso.
- `AnalyticsController.Benchmarking` (GET) sin `[RequirePermission]`.
- `AnalyticsController.Snapshots` (GET) sin `[RequirePermission]`.
- El botón "Agregar ayer" (Aggregate) tiene la fecha hardcodeada en `DateTime.UtcNow.AddDays(-1)`. No hay selector de fecha para agregar un día específico.
- El proceso de Rebuild es fire-and-forget. El usuario no tiene forma de saber si terminó, cuánto tardó, o si falló después de dispararlo.
- La vista de Snapshots no tiene paginación del lado del servidor: carga todos los snapshots del rango.
- El parámetro `cohort` en Benchmarking no tiene UI asociada. El usuario no puede seleccionar un cohorte específico para comparar.

### Faltantes de frontend
- Los gráficos Chart.js tienen un `try/catch` que solo hace `console.error`. Si Chart.js no carga desde CDN, el canvas queda vacío sin mensaje al usuario.
- No existe hoja de estilos de impresión para los gráficos.
- La vista `Index` de Analytics tiene un botón "Aggregate ayer" que no indica si la operación fue exitosa más allá del TempData (que requiere refresh).
- La vista Trends no tiene selector de `days` (solo `from`/`to`), a diferencia de Index que sí tiene ambos.
- No existe exportación de datos de trends/snapshots a CSV/Excel.

### Faltantes de backend
- Sin try/catch en ninguna acción del controlador de Analytics. Si cualquier servicio falla, el usuario recibe un 500 sin mensaje.
- Sin validación de `from <= to` en filtros de fecha.
- El parámetro `days` está clampeado en `Index` pero no en `Trends`, `Benchmarking` o `Snapshots`.
- El permiso `PermissionsView` requerido en Index no está en `Trends`, `Benchmarking` y `Snapshots` a pesar de manejar el mismo nivel de datos.

### Faltantes de validación
- Sin validación de que el rango de fechas sea razonable (ej. máximo 1 año de datos).
- Sin validación de `from <= to` en ninguna acción.

### Faltantes de integración
- El proceso de Rebuild no expone un endpoint de estado (polling) para que el frontend pueda mostrar progreso.
- La configuración de Analytics (`Settings`) no está vinculada visualmente a qué módulos están activos en el plan del tenant.

### Faltantes de UX/UI
- Sin indicador de progreso durante Rebuild.
- Sin mensaje cuando no hay datos suficientes para mostrar un gráfico.
- Sin botón para descargar los datos del gráfico como CSV.
- Las fechas en ejes de los gráficos no respetan el formato de fecha del tenant.

### Faltantes para cierre del módulo
- `[RequirePermission]` en Trends, Benchmarking y Snapshots.
- Try/catch en todas las acciones.
- Validación `from <= to`.
- Indicador de progreso de Rebuild.
- Exportación de datos de snapshots.
- UI para selección de cohorte en Benchmarking.

---

## 11. Automatizaciones (Automations / Workflows)

### Faltantes funcionales
- El payload template y la retry policy están hardcodeados como JSON string en el controlador (líneas 55-56). El usuario no puede personalizar estos valores desde la UI en la creación inicial.
- No existe validación de que `HeadersJson`, `PayloadTemplateJson` y `RetryPolicyJson` sean JSON válido antes de guardar. Solo se verifica que no sean null.
- `TestWebhook` usa `HttpContext.RequestServices.GetRequiredService<IWorkflowTestService>()` (service locator anti-pattern). Esto es funcional pero indica implementación incompleta.
- No existe vista de historial de ejecuciones vinculada desde el listado de automatizaciones. El botón que lo haría no existe en `Index.cshtml`.
- No existe duplicación/clonación de una automatización existente.
- No existe validación de que la URL del webhook sea alcanzable antes de guardar.
- Los resultados del Test de webhook se muestran como TempData (requieren recarga de página), no inline.

### Faltantes de frontend
- `Index.cshtml`: La URL del webhook se trunca con CSS pero no hay botón de "copiar al portapapeles".
- `Index.cshtml`: Las métricas de ejecución (Ejecutadas, Exitosas, Fallidas, Tiempo promedio) son globales sin filtro de fecha.
- `Index.cshtml`: El código del workflow se muestra en una columna `<code>` pero sin estilizado de monospace diferenciado.
- `Index.cshtml`: No hay enlace directo al historial de ejecuciones de cada workflow individual.
- `Create.cshtml`/`Edit.cshtml`: No hay editor visual de JSON para el payload template y retry policy. Son campos textarea que requieren JSON válido manualmente.
- No existe validación cliente de JSON en los campos de headers/payload/retry.

### Faltantes de backend
- Sin validación de JSON válido en `HeadersJson`, `PayloadTemplateJson`, `RetryPolicyJson`.
- El código del workflow se genera reemplazando espacios por guiones (línea 139): no valida caracteres especiales, acentos u otros caracteres inválidos.
- No existe auditlog de cambios en automatizaciones (creación, activación/desactivación, eliminación).

### Faltantes de validación
- Sin validación de formato URL en `WebhookUrl` (solo `[Required]`).
- Sin validación de JSON en campos JSON.
- Sin validación de que el `EventType` seleccionado exista en la lista de eventos soportados (solo se valida que no sea vacío).

### Faltantes de integración
- No existe integración visual entre la automatización y sus ejecuciones en `WorkflowExecutions`. El módulo existe separado pero no hay navegación directa desde la automatización.

### Faltantes de UX/UI
- Sin editor de JSON visual.
- Sin botón "Copiar URL" del webhook.
- Sin resultado inline del test de webhook.
- Sin filtro de métricas por fecha.
- Sin búsqueda en el DataTable de automatizaciones.

### Faltantes de seguridad o control
- Sin auditlog de cambios en workflows.
- Sin validación de JSON puede permitir guardar templates malformados que rompan la ejecución.

### Faltantes para cierre del módulo
- Validación de JSON en campos de template.
- Enlace a historial de ejecuciones por workflow.
- Resultado inline del test de webhook.
- Editor de JSON o al menos validación en tiempo real en el formulario.
- Botón de clonar workflow.

---

## 12. Ejecuciones de Workflow (WorkflowExecutions)

### Faltantes funcionales
- No existe filtro por automatización específica en el listado de ejecuciones. Se ven todas mezcladas.
- No existe filtro por rango de fechas en el listado.
- `Details` view fue referenciada pero no analizada en profundidad. La acción `Retry` existe pero no está claro si el botón existe en la vista.
- No existe cancelación de ejecuciones en curso.
- No existe descarga del log de ejecución.

### Faltantes de frontend
- Sin filtro por automatización, estado o fecha.
- Sin exportación del historial de ejecuciones.

### Faltantes de backend
- `Retry` acción: sin validación de que la ejecución esté en estado fallido antes de reintentar.

### Faltantes para cierre del módulo
- Filtros por automatización, estado y fecha.
- Botón de Retry visible y condicionado por estado.
- Descarga de log de ejecución.

---

## 13. Administración de Usuarios (AdminUsers)

### Faltantes funcionales
- No existe acción de eliminar usuario (solo desactivar con `SetActive`).
- No existe acción de desbloquear usuario. El campo `IsLocked` existe en el modelo y en la vista del listado se muestra el estado, pero no hay botón para cambiar ese estado.
- No existe acción de restablecer contraseña. El usuario solo puede cambiarse la contraseña si el administrador sabe la contraseña actual (a través de la edición), lo cual es incorrecto.
- No existe acción de enviar correo de bienvenida al usuario creado.
- `SetActive` solo es accionable desde la vista `Details`, no desde el listado `Index`.
- No existe asignación de múltiples roles por usuario en una sola operación.

### Faltantes de frontend
- `Index.cshtml`: Sin botón de "Restablecer contraseña" por fila.
- `Index.cshtml`: Sin botón de "Desbloquear" cuando `IsLocked = true`.
- `Index.cshtml`: Sin acciones en lote (batch deactivation, batch role assignment).
- `Index.cshtml`: Sin columna de "Último acceso" o "Fecha de creación".
- `Index.cshtml`: Los botones de acción son solo íconos sin etiqueta de texto (problema de accesibilidad).
- El estado "Activo/Inactivo" se muestra como badge pero no hay acción rápida desde el listado para cambiarlo.

### Faltantes de backend
- No existe endpoint de "Unlock User".
- No existe endpoint de "Reset Password" (send password reset email).
- No existe registro en AuditLog de creación/modificación de usuarios desde el controlador.
- La validación de contraseña en `Create` solo verifica que no sea nula/vacía. La complejidad de contraseña depende de las reglas de Identity configuradas, pero no se muestra retroalimentación al usuario sobre los requisitos.

### Faltantes de validación
- Sin validación de que el correo no exista ya en el tenant al crear un nuevo usuario.
- Sin confirmación de contraseña en el formulario de creación (solo campo `Password`, sin `ConfirmPassword`).

### Faltantes de UX/UI
- Sin acciones en lote.
- Sin botón de desbloqueo visible cuando el usuario está bloqueado.
- Sin botón de reset de contraseña.
- Sin fecha de creación/último acceso en el listado.

### Faltantes de seguridad o control
- Sin AuditLog de operaciones de usuario desde el controlador.
- Sin confirmación de contraseña en creación.

### Faltantes para cierre del módulo
- Acción "Desbloquear usuario".
- Acción "Restablecer contraseña" (envío de email o generación de link).
- Confirmación de contraseña en formulario de creación.
- Registro en AuditLog.
- Acciones de `SetActive` desde el listado.

---

## 14. Administración de Roles y Permisos (AdminRoles)

### Faltantes funcionales
- La asignación de permisos a un rol es una operación separada del rol en sí (requiere dos pasos: crear rol → asignar permisos). No existe un flujo unificado.
- No existe duplicación de rol (clonar un rol con sus permisos).
- `Delete` elimina el rol de forma permanente sin verificar si hay usuarios asignados a ese rol.
- No existe vista de "qué usuarios tienen este rol" desde la pantalla del rol.
- No existe historial de cambios de permisos por rol.

### Faltantes de frontend
- La confirmación de eliminación de rol usa un `<form>` submit directo (sin modal de confirmación). Es fácil eliminar un rol accidentalmente.
- No hay indicador de cuántos usuarios tienen cada rol en el listado.
- No hay búsqueda/filtro en el listado de roles.

### Faltantes de backend
- `Delete` no verifica si el rol tiene usuarios asignados antes de eliminar.
- No existe AuditLog de cambios de permisos en roles.
- La asignación de permisos no es atómica con la creación del rol.

### Faltantes de validación
- Sin validación de que el nombre del rol sea único.
- Sin validación de que no se eliminen roles del sistema (ej. Admin, SuperAdmin).

### Faltantes para cierre del módulo
- Verificar usuarios asignados antes de eliminar.
- Modal de confirmación para eliminación.
- Indicador de usuarios por rol.
- AuditLog de cambios de permisos.
- Validación de roles de sistema protegidos.

---

## 15. Plantillas de Notificación (NotificationTemplates)

### Faltantes funcionales
- Los campos de la plantilla se muestran todos simultáneamente independientemente del canal seleccionado (`Channel`). Un canal WhatsApp no necesita `FromEmail`, `SubjectTemplate`, etc., pero el formulario los muestra igualmente.
- No existe previsualización de la plantilla con variables de ejemplo.
- No existe prueba de envío de notificación con la plantilla activa.
- No existe validación de que las variables usadas en `BodyTemplate` (ej. `{{patient_name}}`) sean variables soportadas por el sistema.
- La vista de Preferencias (`Preferences`) existe en el controlador pero no fue analizada. Se desconoce qué campos tiene y si está completa.
- Los campos de correo (`FromEmail`, `ReplyTo`) no tienen validación de formato en el backend más allá de anotaciones básicas.

### Faltantes de frontend
- Sin visibilidad condicional de campos según `Channel` seleccionado.
- Sin editor visual para `HtmlBodyTemplate` (textarea puro para HTML).
- Sin botón de prueba de envío.
- Sin previsualización de la plantilla renderizada con variables de ejemplo.
- Los EventType y Channel se muestran como nombres de enum en inglés, sin etiquetas localizadas al español.

### Faltantes de backend
- Sin validación channel-específica: si `Channel = Email`, debe requerir `SubjectTemplate` y `BodyTemplate`. Si `Channel = Webhook`, debe requerir `WebhookUrl`.
- Sin validación de que `WebhookUrl` sea una URL válida cuando el canal es Webhook.
- Sin validación de que `WebhookMethod` sea un verbo HTTP válido (GET, POST, PUT, PATCH).

### Faltantes de validación
- Sin validación condicional por canal.
- Sin validación de variables en el cuerpo de la plantilla.

### Faltantes para cierre del módulo
- Validación condicional por canal en backend y visibilidad condicional en frontend.
- Previsualización de plantilla.
- Prueba de envío.
- Análisis y validación de variables en templates.

---

## 16. Portal del Paciente — General / Home

### Faltantes funcionales
- El dashboard del portal (`Home/Index`) muestra 3 tarjetas: próxima cita, estado de cuenta, notificaciones. No hay historial de visitas, resumen de expediente, ni documentos.
- Los accesos rápidos son estáticos y no configurables.
- No existe sección de documentos del paciente (resultados de laboratorio, recetas exportadas).

### Faltantes de frontend
- No hay resumen de signos vitales del último expediente.
- No hay enlace a la historia clínica desde el portal del paciente.
- La sección de notificaciones muestra "Sin leer" como texto hardcodeado, no el conteo real en la tarjeta.
- Los accesos rápidos muestran 4 íconos, pero no todos los módulos disponibles tienen acceso rápido.

### Faltantes de UX/UI
- Sin bienvenida personalizada con fecha de última visita.
- Sin resumen de próximos pagos o deuda pendiente en un widget prominente.

### Faltantes para cierre del módulo
- Al menos mostrar último expediente resumido (fecha, diagnóstico principal).
- Conteo real de notificaciones no leídas en el widget de la tarjeta.

---

## 17. Portal del Paciente — Citas

### Faltantes funcionales
- `History` (GET) en el controlador existe pero no hay referencia confirmada a `History.cshtml`. Si la vista existe, el análisis de su contenido fue omitido. Si no existe, la acción retorna sin vista.
- El formulario de cancelación envía `motivo=""` (string vacío hardcodeado en el formulario, `value=""`). No hay campo de texto para que el paciente ingrese el motivo. El backend recibirá siempre un motivo vacío.
- No existe capacidad de reprogramar una cita desde el portal.
- No existe capacidad de solicitar una nueva cita desde el portal (solo ver las existentes).
- No existe paginación en `History` (hardcodeado a 50 registros en el controlador).
- No existe vista de detalle de una cita individual desde el portal.

### Faltantes de frontend
- Sin modal para ingresar motivo de cancelación.
- Sin opción de reprogramación.
- Sin vista de historial confirmada.
- Sin información del consultorio/sala de la cita.
- Sin opción de agregar cita al calendario del dispositivo (iCal/Google Calendar).

### Faltantes de validación
- Sin requerir motivo de cancelación (se envía vacío siempre).

### Faltantes para cierre del módulo
- Modal de cancelación con campo de motivo real.
- Vista `History.cshtml` (si no existe, crearla).
- Vista de detalle de cita.
- Paginación del historial.

---

## 18. Portal del Paciente — Perfil

### Faltantes funcionales
- La edición del perfil no verifica en el controlador que el `PatientId` editado corresponda al paciente autenticado (se confía en el service layer).
- No existe cambio de contraseña desde el portal del paciente.
- No existe cambio de correo electrónico (es el identificador de acceso).
- No existe subida de foto de perfil.

### Faltantes de backend
- Sin verificación de ownership en el controlador (solo en service layer, no visible en controlador).
- Sin AuditLog de actualización de perfil.

### Faltantes de validación
- Sin validación que el correo actualizado no pertenezca ya a otro paciente/usuario.

### Faltantes para cierre del módulo
- Verificación explícita en controlador de que el paciente solo edita su propio perfil.
- Cambio de contraseña desde el portal.

---

## 19. Portal del Paciente — Facturación

### Faltantes funcionales
- El portal muestra facturas y pagos pero no hay forma de pagar desde el portal. Solo es lectura.
- `AccountStatus` en el controlador retorna `View()` con solo ViewData (sin modelo tipado). Esto puede causar errores en la vista si se intenta acceder a propiedades de un modelo inexistente.
- No existe descarga/exportación de facturas en PDF desde el portal.
- La lista de facturas y la lista de pagos no tienen paginación en el controlador (límite de 50 hardcodeado).

### Faltantes de frontend
- Sin botón de pago en línea.
- Sin descarga de PDF de factura.
- Sin filtro de facturas por fecha o estado.

### Faltantes de backend
- `AccountStatus` retorna `View()` sin modelo tipado.
- Sin paginación configurable.

### Faltantes para cierre del módulo
- Modelo tipado en `AccountStatus`.
- Paginación real.
- Al menos exportación de factura a PDF.

---

## 20. Portal del Paciente — Notificaciones

### Faltantes funcionales
- No existe eliminación de notificaciones.
- No existe marcado masivo como leídas.
- La acción `MarkRead` no devuelve feedback al usuario (no hay TempData de éxito/error en la respuesta).
- El parámetro `returnUrl` en `MarkRead` no está validado contra open redirect.

### Faltantes de seguridad o control
- `returnUrl` sin validación es una vulnerabilidad de open redirect.

### Faltantes para cierre del módulo
- Validación de `returnUrl` con `Url.IsLocalUrl()`.
- TempData de confirmación en `MarkRead`.
- Opción de marcar todas como leídas.

---

## 21. Módulo de IA — Copilot

### Faltantes funcionales
- `CopilotController.Query` (POST) no valida la longitud ni el contenido de la consulta. Un usuario puede enviar una consulta vacía o extremadamente larga.
- No existe historial de consultas al copilot.
- No existe rate limiting visible en el endpoint del copilot.
- No existe timeout visible si el servicio de IA tarda demasiado.
- El `Index` (GET) no pasa ningún modelo a la vista; la vista se renderiza completamente estática sin contexto del tenant.

### Faltantes de frontend
- `Copilot/Index.cshtml`: El resultado de la consulta se inserta con concatenación de HTML en JavaScript (`innerHTML`). Si el backend devuelve caracteres especiales o el título de una entidad contiene HTML, hay riesgo de XSS.
- Sin indicador de carga (spinner) mientras se espera la respuesta.
- Sin manejo de error AJAX: si el servidor devuelve 500 o 401, el AJAX no muestra mensaje al usuario.
- Sin validación de longitud del campo de consulta en el cliente.
- Sin historial de consultas recientes.

### Faltantes de backend
- Sin validación de longitud de consulta.
- Sin rate limiting.
- Sin try/catch en `Query` POST. Si `_copilot.QueryAsync()` lanza excepción, el usuario recibe 500.
- Sin logging de consultas para métricas de uso.

### Faltantes de seguridad o control
- XSS potencial en renderizado de resultados.
- Sin rate limiting.

### Faltantes para cierre del módulo
- Validación de longitud de consulta (backend + frontend).
- Manejo de errores AJAX con mensaje al usuario.
- Try/catch en `Query` POST.
- Sanitización de HTML en resultados antes de insertar en DOM (usar `textContent` o escaping).
- Indicador de carga.

---

## 22. Módulo de IA — Insights

### Faltantes funcionales
- No existe exportación del listado de insights.
- No existe creación manual de insights por parte del usuario.
- No existen acciones en lote (acknowledge o dismiss múltiples).
- No existe filtro guardado/preset.
- Los parámetros `minScore` y `minConfidence` en el filtro no tienen validación de rango (0.0 a 1.0).
- Los parámetros `from`/`to` no tienen validación de que `from <= to`.

### Faltantes de frontend
- Sin exportación.
- Sin bulk actions.
- Sin validación de rango para score y confidence en el filtro.

### Faltantes de backend
- Sin validación de `from <= to`.
- Sin validación de rango en `minScore` y `minConfidence`.
- Los redirects post-Acknowledge y post-Dismiss usan fallback hardcodeados.

### Faltantes para cierre del módulo
- Validación de filtros.
- Exportación de insights.
- Bulk actions.

---

## 23. Módulo de IA — Dashboard y Configuración

### Faltantes funcionales
- `AIDashboardController` existe pero no fue analizado en detalle. Se desconoce si la acción `Refresh` funciona correctamente o es un stub.
- `AISettingsController` existe pero su vista no fue analizada. Se desconoce qué opciones de configuración expone y si todas están conectadas al backend.
- `RecommendationsController` existe (`Index`, `Apply`) pero no fue analizado. No se sabe si `Apply` tiene implementación real o es un stub.

### Faltantes de integración
- No existe integración visual entre los Insights de IA y el Dashboard ejecutivo principal.
- Las recomendaciones de IA no tienen flujo confirmado de aplicación (qué hace "Apply" exactamente).

### Faltantes para cierre del módulo
- Confirmar implementación de `AIDashboardController.Refresh`.
- Confirmar implementación de `RecommendationsController.Apply`.
- Confirmar que `AISettingsController` expone y guarda todas las configuraciones de AI del tenant.

---

## 24. Onboarding (Wizard Multi-paso)

### Faltantes funcionales
- `OnboardingStep3Vm.Password` no tiene atributos de validación de complejidad (`[StringLength]`, `[RegularExpression]`). La vista muestra el texto "Mín. 6 caracteres, mayúscula, minúscula y dígito", pero el modelo no lo valida. Esta promesa de UI no está respaldada por el backend.
- El código del tenant (`Code`) se valida de unicidad solo en el Step final (provision), no durante el Step 1. El usuario puede llegar al paso 5 y fallar al final sin poder volver y corregir el código sin perder otros datos.
- El checkbox "Iniciar con prueba gratuita" se desactiva silenciosamente si el plan no tiene días de prueba (`TrialDays <= 0`), sin mostrar mensaje de validación. El usuario no sabe por qué se ignoró su selección.
- Los límites del plan (MaxUsers, MaxDoctors, MaxPatients, MaxAppointmentsPerMonth) se muestran en el Step 2 pero no existe confirmación de que se apliquen después del onboarding.
- Los dropdowns de Step 4 (TimeZone, DateFormat, Currency, Language) son hardcodeados en el controlador como listas estáticas. No hay configuración en base de datos que permita agregar nuevas opciones sin código.

### Faltantes de frontend
- El partial `_WizardProgress` que muestra el indicador de paso (1/5, 2/5...) es referenciado en todos los steps pero su existencia no fue confirmada. Si no existe, el wizard no muestra progreso visual.
- No existe indicación de que las opciones de Step 4 se pueden cambiar después del onboarding en Configuración.
- No existe validación AJAX de disponibilidad del código en Step 1 (el usuario no sabe si el código está tomado hasta el submit final).

### Faltantes de validación
- Sin validación de complejidad de contraseña en el modelo de Step 3.
- Sin validación de unicidad de código en Step 1 (pre-provision).
- Sin validación de que el checkbox trial corresponda a un plan con `TrialDays > 0`.

### Faltantes de UX/UI
- Sin feedback cuando el código ya está en uso hasta el submit final.
- Sin feedback claro cuando el trial se desactiva silenciosamente.
- Sin indicador de progreso confirmado (`_WizardProgress`).

### Faltantes para cierre del módulo
- Añadir validación de complejidad al modelo de Step 3.
- Validación AJAX de disponibilidad de código en Step 1.
- Mensaje visible cuando trial se desactiva.
- Confirmar existencia de `_WizardProgress` partial.

---

## 25. SuperAdmin — Gestión de Tenants

### Faltantes funcionales
- La vista `Details` del tenant no fue analizada. No se confirma qué acciones de gestión (Suspend, Activate, ChangePlan) tienen formularios en la vista. El controlador tiene los endpoints pero la UI puede ser incompleta.
- `TenantsController.Create` (POST) no valida unicidad del `Code` antes de llamar al servicio de provisión. Si el código ya existe, el error solo se muestra si el servicio lo devuelve.
- No existe búsqueda o filtro avanzado en el listado de tenants (solo DataTable con búsqueda de cliente).
- No existe exportación del listado de tenants.
- No existe vista de métricas del tenant (uso actual vs. límites del plan).
- No existe acción de migrar datos de un tenant a otro plan con validación de límites.

### Faltantes de frontend
- El listado usa columnas `TrialEndDate` y `EndDate` formateadas como `yyyy-MM-dd` hardcodeado.
- No hay acciones rápidas (Suspend/Activate) desde el listado. Todo requiere ir al detalle.
- Sin indicador de cuántos usuarios/doctores/pacientes tiene el tenant respecto a sus límites del plan.

### Faltantes de backend
- Sin validación de unicidad de código en `Create` POST (depende del servicio).
- Sin AuditLog de acciones administrativas sobre tenants (Suspend, Activate, ChangePlan).

### Faltantes para cierre del módulo
- Confirmar y completar acciones Suspend/Activate/ChangePlan en la vista Details.
- Validación de código único en Create.
- Métricas de uso del tenant en Details.
- AuditLog de acciones administrativas.

---

## 26. SuperAdmin — Suscripciones y Planes

### Faltantes funcionales
- `SubscriptionsController` y `PlansController` existen pero no fueron analizados en detalle. Se desconoce si las vistas Details, Upgrade, Downgrade y Cancel están implementadas.
- No existe calculadora de impacto de cambio de plan (cuántos recursos quedarían fuera de límite al bajar de plan).
- No existe historial de cambios de plan visible en la UI de suscripciones.

### Faltantes para cierre del módulo
- Confirmar implementación completa de Upgrade/Downgrade/Cancel con sus vistas.
- Añadir historial de cambios de plan.
- Calculadora de impacto en downgrade.

---

## 27. SuperAdmin — Billing SaaS

### Faltantes funcionales
- `BillingController` en SuperAdmin existe pero no fue analizado en detalle.
- Se desconoce si la exportación de transacciones funciona o es un stub.
- No existe integración visual con Stripe dashboard (solo logs de webhooks en Ops).

### Faltantes para cierre del módulo
- Confirmar implementación de Export de transacciones.
- Verificar que Invoice view muestra datos reales de SaaSInvoice.

---

## 28. Área Ops — Health Dashboard y Workers

### Faltantes funcionales
- `HealthDashboardController` y `WorkersController` existen pero sus vistas y acciones no fueron analizadas en detalle.
- No se confirma si el listado de Workers muestra el estado real de `WorkerHeartbeat` en tiempo real o con un delay.
- No existe acción para reiniciar un worker desde la UI.
- No existe exportación del log de salud o de webhooks.

### Faltantes para cierre del módulo
- Confirmar implementación de vistas de HealthDashboard y Workers.
- Acción de reinicio/control de workers si aplica.

---

## 29. API Móvil

### Faltantes funcionales
- No existe documentación de API (Swagger/OpenAPI) accesible. Si existe, no está confirmada en la configuración visible.
- `MobilePaymentsController` existe pero sus métodos no fueron analizados. Se desconoce si el flujo de pago móvil está implementado.
- No existe endpoint de sincronización offline para la app móvil.
- No existe versioning visible más allá de `/v1/` en las rutas.

### Faltantes de backend
- Sin documentación de contratos de API.
- El manejo de errores JWT expirado y refresh token no fue confirmado como correcto en todos los endpoints.

### Faltantes de seguridad o control
- Sin rate limiting visible en endpoints móviles.
- Sin documentación de scopes o claims requeridos por endpoint.

### Faltantes para cierre del módulo
- Swagger/OpenAPI habilitado al menos en entorno de desarrollo.
- Confirmar implementación de `MobilePaymentsController`.
- Confirmar flujo de refresh token en todos los endpoints protegidos.

---

## 30. Seguridad y Autenticación (Staff)

### Faltantes funcionales
- No existe flujo de recuperación de contraseña para usuarios del staff. Si un usuario olvida su contraseña, no hay endpoint ni vista de "Olvidé mi contraseña".
- No existe 2FA (autenticación en dos factores) para usuarios del staff.
- No existe bloqueo automático por intentos fallidos de login (a menos que esté en Identity config, pero no está confirmado en el controlador).
- No existe sesión con expiración configurable por tenant.

### Faltantes de frontend
- Sin enlace "¿Olvidaste tu contraseña?" en la página de login del staff.

### Faltantes de backend
- Sin endpoint de "ForgotPassword" / "ResetPassword" en `AccountController`.
- Sin 2FA.

### Faltantes para cierre del módulo
- Flujo completo de recuperación de contraseña.
- Confirmación de que el bloqueo por intentos fallidos está configurado en Identity.

---

## 31. Configuración General (Settings)

### Faltantes funcionales
- `SettingsController` existe pero su vista `Index.cshtml` no fue analizada. No se confirma qué configuraciones expone.
- No existe preview de cambios de configuración antes de aplicar.
- No existe historial de cambios de configuración del tenant.

### Faltantes para cierre del módulo
- Confirmar que `Settings/Index` expone y guarda: TimeZone, DateFormat, Currency, Language, y configuraciones de módulos.
- Añadir AuditLog de cambios de configuración.

---

## 32. Navegación y Layout Global

### Faltantes funcionales
- El sidebar de administración no fue analizado directamente. Basado en los módulos existentes, es posible que módulos como `EventLogs`, `CashMovements`, `TenantBilling`, y `Permissions` no estén visibles o accesibles desde el menú lateral.
- No existe breadcrumb dinámico generado por código. Los breadcrumbs se setean manualmente con `ViewData["Breadcrumb"]` en cada controlador. Si algún controlador omite esto, la navegación queda sin contexto.
- No existe navbar superior con notificaciones del staff (solo del portal del paciente).
- No existe búsqueda global desde el header (buscar pacientes, citas, facturas desde cualquier módulo).

### Faltantes de frontend
- Las dependencias de CDN (`Chart.js`, `DataTables i18n`, `Select2`) están referenciadas desde URLs externas. En un entorno sin internet, todos estos componentes fallan. No hay fallback local.
- No hay confirmación de que `select2` CSS y JS estén incluidos en el layout principal. Múltiples vistas lo usan (BillingInvoices, Payments, MedicalRecords).
- No existe CSS de impresión global para ocultar sidebars y navbars.

### Faltantes de UX/UI
- Sin búsqueda global.
- Sin notificaciones internas del staff en el navbar.
- Sin modo oscuro o preferencia de tema.
- Sin accesibilidad ARIA en botones de acción de íconos (varios botones son íconos sin `aria-label`).

### Faltantes de seguridad o control
- Las dependencias de CDN no están subresource-integrity (SRI) hasheadas. Un CDN comprometido puede inyectar código malicioso.

### Faltantes para cierre del módulo
- Bundlear o servir localmente Chart.js, Select2, DataTables.
- Añadir SRI hashes a dependencias CDN.
- Añadir `aria-label` a botones de acción.
- Verificar que todos los módulos están accesibles desde la navegación.

---

## 33. Persistencia y Datos

### Faltantes funcionales
- No existe seed de datos de demostración funcional para entornos de desarrollo (el `DataSeeder.cs` tiene modificaciones no committed según el git status).
- No existe migración documentada para añadir nuevas configuraciones del tenant sin romper el schema existente.
- Las entidades `TenantDailySnapshot` y `AnalyticsJobLog` no tienen una política de retención de datos definida en código. Pueden crecer indefinidamente.

### Faltantes de backend
- `DataSeeder.cs` tiene cambios pendientes sin commitear. El estado real del seeder es incierto.
- No existe estrategia visible de archivado de datos antiguos (citas, expedientes, logs).

### Faltantes para cierre del módulo
- Política de retención para snapshots y logs.
- Confirmar estado y completitud del `DataSeeder.cs`.
- Documentar proceso de migración para tenants existentes.

---

*Fin del análisis. Total de módulos revisados: 33.*
*Basado en lectura directa de código fuente. Fecha: 2026-04-02.*
