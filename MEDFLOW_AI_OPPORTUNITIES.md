# MedFlow — IA: qué existe y qué monetizar / automatizar

**Base de código:** interfaces en `MedFlow.Application/Interfaces/AI/`, área `Areas/AI`, entidades `AIInsight`, enums `AIInsightType`, `AISeverity`, procesadores referenciados en docs.

**Base documental:** `ANALISIS_FALTANTES_MODULO_A_MODULO.md` §21–23, `ANALISIS_SUPREMO_SISTEMA.md`.

---

## 1. Qué IA **ya existe** (real)

| Capacidad | Rol |
|-----------|-----|
| **Copilot operacional** | Asistente consultas staff (`CopilotController`) |
| **Insights** | Alertas/insights derivados de datos (`InsightsController`, servicios insight) |
| **Procesamiento batch insights** | `IAIInsightProcessorService` |
| **Motor recomendaciones** | `IRecommendationEngine`, UI Recommendations |
| **Riesgo no-show / pagos / engagement** | Servicios dedicados (`INoShowRiskService`, `IPaymentRiskService`, `IPatientEngagementService`) |
| **Proveedores IA** | `IAIModelProvider`, `IInferenceProvider`, `IRiskScoringProvider` |
| **Configuración por tenant** | `AISettingsService`, keys en dominio |

---

## 2. Qué IA **falta** para producto líder

| Gap | Por qué importa |
|-----|-----------------|
| **Guardrails clínicos + auditoría** | Confianza legal y médica |
| **Evaluación sistemática** | Sin métricas = sin venta enterprise |
| **Integración dashboard ejecutivo** | Insights aislados = menor ROI percibido |
| **Rate limit + timeouts Copilot** | Abuso y costos (`ANALISIS_FALTANTES` §21) |
| **Mitigación XSS en respuesta Copilot** | Riesgo seguridad documentado |
| **Historial y telemetría de uso** | Facturación por uso y mejora modelo |

---

## 3. IA que **genera dinero**

| Idea empaquetable | Modelo de cobro |
|-------------------|----------------|
| **Copilot Business+** | Por usuario/mensaje/mes |
| **Insights Pro** | Por volumen de insights o severity threshold |
| **Predicción no-show accionable** | Add-on por sede |
| **Priorización cobranza** | Add-on financiero |
| **Copilot API** | Para partners integradores |

---

## 4. IA que **automatiza clínicas**

- Priorización de llamadas recordatorio según riesgo no-show.
- Sugerencia de huecos de agenda (requiere motor reglas + datos limpios).
- Resumen pre-consulta para médico (con citación a expediente).

---

## 5. IA que **impresiona** en demo

- Resumen “estado del paciente” en 10 segundos con fuentes enlazadas.
- Simulador “impacto en ingresos si reduce no-show X%”.

---

## 6. IA que **hace crecer el negocio MedFlow**

- **Land-and-expand:** insights muestran valor → upgrade plan.
- **Partners:** API IA limitada por API key tier.
- **Datos agregados anonimizados** (solo con marco legal ético) para benchmarks premium.

---

## 7. Prioridad técnica inmediata (deuda documentada)

1. Copilot: validación longitud, try/catch, escaping/XSS, spinner, rate limit.
2. Insights: validación filtros, export, bulk acknowledge.
3. Integración visual con dashboard principal.

---

## 8. Conclusión

MedFlow **no es IA cosmética**: hay **capas de servicio** y **UI**. El salto comercial viene de **confianza** (seguridad + calidad medida) y **acciones en producto**, no solo texto generado.

---

*Política de uso de datos sensibles debe revisarse con abogado sector salud por jurisdicción.*
