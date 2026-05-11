# MedFlow — Qué falta para ser “el mejor” (en su categoría objetivo)

**Definición honesta de “el mejor”:** líder en **SMB/región** en **ROI operativo + experiencia premium + confianza**, no superar Epic global sin inversión incompatible.

**Fuentes:** los cuatro documentos base + hallazgos repo (sin `.github/workflows`).

---

## 1. Producto / UX

| Falta | Impacto |
|-------|---------|
| **Design system propietario** | Romper sensación plantilla AdminLTE |
| **Estados vacíos y errores de primera clase** | Confianza y conversión |
| **Dashboard drill-down accionable** | Dirección toma decisiones rápido |
| **Exports Excel/PDF serios** donde el doc marca gap | Ventas finance/reception |
| **Unificar portal paciente** (rutas duales) | Menos confusión y soporte |
| **Mobile**: PWA premium o apps nativas sobre API existente | Mercados donde doctor usa teléfono |

---

## 2. Ingeniería / plataforma

| Falta | Impacto |
|-------|---------|
| **CI/CD visible** (pipelines, gates migración) | Velocidad equipo + calidad |
| **SLIs/SLOs** y alertas en prod | Enterprise credibility |
| **Caching estratégico dashboard** | Performance y costo DB |
| **End sharding** documentado | Solo si tracción masiva |

---

## 3. Seguridad / compliance

| Falta | Impacto |
|-------|---------|
| **Endurecer CORS por defecto** en prod | Riesgo documentado en supremo |
| **Cerrar lagunas GET permisos** si quedan (`ANALISIS_FALTANTES` histórico) | Data breach reputacional |
| **Programa formal** SOC2/GDPR/HIPAA según mercado | Venta enterprise |
| **Pentest externo** | Requisito comprador serio |

---

## 4. Dominio clínico / interoperabilidad

| Falta | Impacto |
|-------|---------|
| **FHIR selectivo** o conectores | Integración red clínica |
| **Telemedicina nativa** | Ingresos nuevos vertical |
| **Laboratorio/imagen** si competís amplio | Tabla comparativa enterprise |

---

## 5. IA

Ver `MEDFLOW_AI_OPPORTUNITIES.md`: Copilot hardening, métricas, integración ejecutiva.

---

## 6. Negocio / go-to-market

| Falta | Impacto |
|-------|---------|
| **Vertical ganador claro** | Evitar dispersión |
| **ROI calculator** vendible | Reduce ciclo ventas |
| **Partner channel** | Escala sin headcount lineal |

---

## 7. Qué haría **dependencia** del producto (stickiness)

1. **Datos históricos ricos** + reporting imposible de migrar rápido.
2. **Workflows críticos** en producción (puerta N8n/billing).
3. **Portal paciente + marca** integrados en operación diaria.
4. **Integraciones de pago** y conciliación confiable.

---

## 8. Conclusión

Ser “el mejor” aquí = **triple coincidencia** citada en supremo: **belleza y velocidad percibida**, **fiabilidad demostrable**, **ROI económico medible para la clínica**. El código ya permite gran parte; falta **empaque**, **operación**, y **historia de confianza**.

---

*Lista viva: revisar tras cada release mayor.*
