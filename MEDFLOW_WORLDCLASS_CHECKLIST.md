# MedFlow — checklist world-class (operativo)

**Versión:** 1.0 · **Fecha:** 2026-05-10  
**Uso:** Definition of Done por release mayor / gate staging → producción.

---

## Leyenda

- [ ] Pendiente  
- [x] Hecho / verificado en esta línea base cuando aplique

---

## 1. Confianza y flujos P0 (`PRUEBAS_FLUJOS_*`)

- [ ] TP-A01–A06 autenticación/autorización reproducibles
- [ ] TP-B01–B04 pacientes CRUD básico **incl. POST** automatizado o manual firmado
- [ ] TP-C citas crear/editar/cambiar estado sin 500
- [ ] Portal paciente login + página inicio sin fuga a staff
- [ ] Logout estable (QA nota intermitencia automatización — revisar humano)

---

## 2. Seguridad (`MEDFLOW_ENTERPRISE_SECURITY.md`)

- [ ] Matriz completa controllers/APIs con permiso explícito por método
- [ ] CORS producción = orígenes explícitos
- [ ] Rate limiting **on** en prod
- [ ] Secrets fuera de repo; rotación documentada
- [ ] Uploads con política MIME/tamaño única
- [ ] Revisión `@Html.Raw` y contenido usuario

---

## 3. Multi-tenant

- [ ] Tests aislamiento ejecutados en CI
- [ ] Nuevo query revisado por segundo par para filtros tenant

---

## 4. UX premium (`MEDFLOW_UI_UX_REDESIGN.md`)

- [x] Tokens globales (`medflow-theme.css`)
- [x] Capa premium incremental (`medflow-premium.css` importada desde `site.css`)
- [ ] Dark mode toggle + persistencia usuario
- [ ] Empty states en todos los listados P0/P1
- [ ] Skeleton o loading en dashboard y listados pesados

---

## 5. Dashboard ejecutivo

- [x] Error boundary servidor + vista null-safe
- [x] KPIs financieros condicionados `billing.view`
- [ ] Caché servidor dashboard con TTL
- [ ] Rango personalizado fechas (opcional release)

---

## 6. Observabilidad

- [ ] OTLP configurado en staging con traces visibles
- [ ] Alertas mínimas: error rate, latencia p95, health failing

---

## 7. CI/CD (`ANALISIS_SUPREMO` gap)

- [ ] Pipeline: restore → build → test → artefacto
- [ ] Migraciones EF revisadas en staging antes prod

---

## 8. IA (`MEDFLOW_AI_ROADMAP.md`)

- [ ] Feature flags por tenant/plan
- [ ] Logs sin PII innecesaria
- [ ] Evaluación offline prompts críticos antes deploy

---

## 9. Portal paciente

- [ ] Responsive auditado en viewports móvil
- [ ] PDF factura/receta descargables sin errores

---

## 10. Documentación

- [x] Roadmap y gaps consolidados (`ROADMAP_FINAL_*`, `MEDFLOW_GAPS_RESTANTES.md`)
- [ ] Runbook incidente tenant leak / rollback DB

---

## Sign-off release

| Rol | Nombre | Fecha |
|-----|--------|-------|
| Engineering lead | | |
| Security champion | | |
| Product owner | | |

---

*Marcar [x] solo con evidencia (ticket, PR, o informe QA enlazado).*
