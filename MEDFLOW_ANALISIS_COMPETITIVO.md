# MedFlow — Análisis competitivo (referencias solicitadas)

**Método:** Comparación por **categorías de valor**, no por checklist infinito de features Epic (sería deshonesto).

**Fuentes internas:** `ANALISIS_SUPREMO_SISTEMA.md`, dominio y APIs inspeccionadas en `src/`.

---

## 1. Epic Systems / Oracle Health / Athenahealth (EHR enterprise)

| Ellos | MedFlow |
|-------|---------|
| Legado institucional, redes hospitalarias, contenido clínico certificado | Enfoque **ambulatorio / clínica SMB–mid-market**, citas–expediente–cobro–SaaS |
| FHIR/HL7 como red | APIs REST propias; **sin FHIR producto** documentado como conformidad completa |
| Compliance vendible a CIO | Base técnica seguridad; **sin programa formal** en repo |

**Qué nos falta:** interoperabilidad profunda, narrativa regulatoria, fuerza de campo enterprise.

**Qué podemos hacer mejor (segmento SMB):** tiempo de implementación, costo total, flexibilidad SaaS, automatización moderna, billing integrado sin proyecto multimillonario.

---

## 2. Salesforce Health Cloud

| Salesforce | MedFlow |
|--------------|---------|
| CRM + journeys enterprise | Pacientes y portal existentes; **no** suite CRM masiva tipo Journey Builder |
| Ecosistema ISV gigante | Sin marketplace maduro |

**Oportunidad MedFlow:** **CRM ligero sector salud** (campamentos, seguimiento post-consulta) como roadmap si hay demanda — hoy es más ERP clínico que CRM puro.

---

## 3. HubSpot

| HubSpot | MedFlow |
|---------|---------|
| Inbound marketing líder | Notificaciones/plantillas sí; **growth marketing omnicanal** no es el core |

**Diferenciador posible:** marketing **ético y compliant** integrado a agenda y facturación — solo con diseño legal fuerte.

---

## 4. Stripe / Shopify / “platform economics”

| Referencia | MedFlow |
|------------|---------|
| Stripe Checkout / DX icónica | Onboarding con opciones; **no** equivalencia documentada de polish universal |
| Shopify ecosystem | Workflows/webhooks = germen; **no** app store |

**Qué nos falta:** developer portal, API keys graduadas por tier, documentación OpenAPI de clase mundial.

---

## 5. Monday / ClickUp / Notion (collaboration)

| Ellos | MedFlow |
|-------|---------|
| UX colaborativa genérica | UX admin eficiente para procesos; **no** doc-first |

**Qué nos falta:** command palette unificado, tiempo real colaborativo.

**Qué podemos igualar:** “lista de trabajo del día” médico/recepción si se diseña workflow-centric UI.

---

## 6. Tabla brutal resumen

| Competidor | Vs MedFlow — brecha principal | Vs MedFlow — ventaja posible |
|------------|-------------------------------|------------------------------|
| Epic/Athena | Alcance hospitalario + compliance | Velocidad, costo, SaaS |
| Salesforce Health | CRM enterprise | Simplicidad operativa SMB |
| Stripe | Pagos/dev UX | Dominio clínica vertical |
| Shopify | Ecosystem apps | Workflows + tenant vertical |
| Monday/Notion | UX moderna genérica | Datos clínico-financieros nativos |

---

## 7. Diferenciadores únicos defendibles (si se ejecutan)

1. **Orquestación clínica-financiera nativa** (cita → expediente → cargo → cobro) en un solo stack tenant-aware.
2. **Automatización + IA operativa** sin proyecto SI gigante.
3. **SaaS Stripe native** para MSP/partners que venden a redes de clínicas.

---

## 8. Conclusión

MedFlow **no compite hoy** en mesa **CIO hospital multinational** sin inversión masiva en compliance/interoperabilidad. **Sí puede competir** donde el buyer es **dueño de clínica / grupo regional**: ROI rápido, costo contenido, producto unificado.

---

*Actualizar cuando exista posición FHIR o certificación formal.*
