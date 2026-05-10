# MedFlow — UI/UX redesign (premium SaaS enterprise)

**Versión:** 1.0 · **Fecha:** 2026-05-10  
**Referencias:** `ANALISIS_SUPREMO_SISTEMA.md` (modernidad ~5.5/10), stack actual AdminLTE 3 + Bootstrap 4 + Razor.

---

## Diagnóstico brutal (UX director + product designer)

Hoy MedFlow **funciona** como consola operativa clínica; **no** transmite la sensación “billonaria” por:

- Patrón dominante **tabla + formulario** sin jerarquía alternativa.
- **AdminLTE** reconocible por usuarios power-user de ERP.
- Dependencias CDN dispersas (DataTables ya mitigado para i18n).
- Estados vacíos y errores **heterogéneos** entre módulos.

El objetivo no es copiar Linear pixel a pixel sino adoptar sus **principios**: densidad controlada, foco, feedback instantáneo, tipografía fuerte, motion mínimo pero intencional.

---

## Pilares del nuevo sistema visual

1. **Tokens primero:** color, spacing, radio, sombra, elevation — ya iniciado en `wwwroot/css/medflow-theme.css`; capa **`medflow-premium.css`** refuerza cards, sidebar, focus rings, hook dark mode.
2. **Tipografía:** Inter como base (cargada en theme); escalas para `h1`, breadcrumbs, tablas.
3. **Superficie:** tarjetas con sombra suave y borde claro; menos “cajas grises pesadas”.
4. **Tablas:** cuando inevitable, headers discretos + hover fila + acciones iconográficas consistentes.
5. **Motion:** 200–250 ms ease-out en hovers y sidebar; sin animaciones distractores clínicos.

---

## Qué eliminar / reducir

| Anti-patrón | Sustituto |
|-------------|-----------|
| Solo tabla como vista principal | Vista resumen (cards KPI mini) + tabla secundaria |
| Breadcrumbs genéricos sin contexto | Subtítulo acción + tenant visible donde aplique |
| Botones “Excel/PDF” falsos o print-only | Rutas reales o esconder hasta existir backend |
| Dark mode “media query accidental” | Toggle usuario + `data-theme` + persistencia BD/preferencia |

---

## Componentes prioritarios a unificar

1. **Page header** — título, subtítulo, acciones primarias alineadas a la derecha (patrón HubSpot).
2. **Empty state** — ilustración ligera o icono grande + copy + CTA único.
3. **Loading** — skeleton para dashboard y listados pesados; spinner solo acciones puntuales.
4. **Toasts / errores** — un solo canal (Toastr ya presente) con códigos de error amigables.
5. **Modales** — SweetAlert2 presente; guidelines de cuándo modal vs página dedicada.

---

## Shell de aplicación (navbar / sidebar)

- **Navbar:** ligero backdrop blur (`medflow-premium.css`) para sensación contemporánea sin romper AdminLTE.
- **Sidebar:** ítems con radio consistente; estado activo claro; agrupación por dominio (Clínica / Finanzas / Admin).
- **Responsive:** prioridad mobile para **portal paciente**; staff tablet-first para agenda.

---

## Dashboard ejecutivo (visión diseño)

- Grid responsivo tipo **Stripe**: KPIs grandes → secundarios → charts.
- **Drill-down:** cada KPI abre filtro pre-aplicado en módulo destino (patrón ya parcialmente en código).
- **Alertas:** severidad con color semántico + acción “Ir a…” cuando exista entidad.

---

## Portal paciente (trato aparte)

Separar identidad visual **consumidor** vs staff: menos densidad, más tarjetas, timeline vertical, descarga PDF evidente.

---

## Plan de migración (sin big-bang)

1. Layout + tokens globales (**hecho incrementalmente**).
2. Dashboard + Pacientes + Citas (máximo contacto usuario).
3. Facturación y caja.
4. Módulos largos cola.

---

## Métricas de diseño

- Task completion time en flujos P0 (antes/después).
- SUS (System Usability Scale) muestral trimestral.
- **% vistas** usando partials compartidos (`_StatCard`, empty states).

---

## Riesgos

- Sobrediseño que **ralentice** médicos en consulta — validar con usuarios reales.
- Dark mode mal contrastado en badges semánticos — revisar WCAG AA donde sea obligatorio por cliente.

---

*Este documento es la brújula visual; el código fuente de verdad son `medflow-theme.css`, `medflow-premium.css`, y vistas Razor migradas progresivamente.*
