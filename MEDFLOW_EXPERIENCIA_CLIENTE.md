# MedFlow — Experiencia cliente (UX/UI/percepción)

**Alcance:** Staff (AdminLTE), paciente (portal dedicado + `/portal`), mobile API como canal — según `ANALISIS_SUPREMO_SISTEMA.md` y QA.

---

## 1. Qué se siente **moderno**

| Elemento | Evidencia |
|----------|-----------|
| **Backend cloud-native ready** | Health checks, OpenTelemetry opcional, JWT API (`Program.cs`) |
| **Dashboard ejecutivo con charts** | Chart.js / ejecutivo JS — datos densos |
| **Portal paciente alternativo** (`patient-portal.css`, tema variables) | Pasos hacia UI menos “ERP” en paciente |
| **Separación de roles** | Sidebars y denegaciones coherentes en QA |
| **Mejoras accesibilidad recientes** | Skip links, foco, landmarks en portal (sesiones recientes de desarrollo) |

---

## 2. Qué se siente **viejo / plantilla**

| Elemento | Por qué |
|----------|---------|
| **AdminLTE + Bootstrap 4** | Asociación fuerte a paneles admin 2015–2020 |
| **Tablas + DataTables** | Eficientes pero **no** sensación app consumidor |
| **Toastr / CDN legacy patterns** | Parcialmente mitigado (DataTables local por CORS en QA) |
| **Muchos formularios densos** | Carga cognitiva alta (`ANALISIS_SUPREMO`) |

---

## 3. Qué parece **premium**

- **Dashboard** cuando hay datos y charts cargados — sensación “command center” SMB.
- **Facturación** con badges de estado y saldos — tono ERP serio.
- **Área IA** — si el usuario cree en insights; el envoltorio UI debe acompañar.
- **Portal paciente** con marca tenant — premium proporcional al branding configurado.

---

## 4. Qué parece **genérico**

- Login staff estándar Identity.
- Listados tabulares sin jerarquía visual fuerte.
- Estados vacíos históricamente débiles (`ANALISIS_FALTANTES` §1 dashboard).

---

## 5. Rendimiento percibido

**Sin SLIs en repo:** inferencia cualitativa desde supremo:

- Dashboard recalcula sin cache documentado → riesgo lentitud con datos grandes.
- Muchos charts en una página → peso front.

**Acción futura:** budgets Web Vitals internos en vistas críticas.

---

## 6. Experiencia **por persona**

| Persona | Sentimiento probable hoy | Mejora de alto impacto |
|---------|--------------------------|-------------------------|
| **Recepción** | Productivo pero ocupado; muchos clics | Acciones rápidas, menos pasos en alta paciente/cita |
| **Doctor** | Expiente usable; búsqueda clave | Vista “mi día”, menos dispersión |
| **Billing** | Fuerte alineación mercado | Exports PDF/excel impecables |
| **Director** | Dashboard prometedor; falta storytelling | Narrativa ejecutiva + drill-down |
| **Paciente** | Funcional; dual `/portal` vs `/PatientPortal` puede confundir | Unificar journey |

---

## 7. Conclusión UX

MedFlow ofrece **funcionalidad amplia** con **envoltorio admin clásico**. El salto “premium” viene de **design system propio**, **microcopy clínico**, **empty states**, **latencia**, y **una sola historia de portal paciente**.

---

*Validación cuantitativa requiere estudios con usuarios reales y analytics de producto.*
