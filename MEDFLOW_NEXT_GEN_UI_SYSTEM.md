# MedFlow Next-Gen UI System

**Objetivo:** Eliminar sensación AdminLTE/ERP antiguo y converger a **Stripe · Linear · Notion · Apple** en **ritmo continuo** (no big-bang).

**Estado implementado (2026-05-10):**

| Artefacto | Función |
|-----------|---------|
| `wwwroot/css/mf-experience-system.css` | Tokens (`--mf-*`), dark theme `html[data-mf-theme="dark"]`, cards `.mf-xp-card`, KPI `.mf-xp-kpi`, skeleton, empty state, paleta overlay |
| `wwwroot/js/mf-shell-experience.js` | `mf-theme` localStorage, `#mfThemeToggle`, `#mfCmdPaletteBtn`, navegación paleta keyboard |
| `Views/Shared/_CommandPalette.cshtml` | Modal **⌘K / Ctrl+K**, lista filtrable |
| `Views/Shared/_AdminNavbar.cshtml` | Botones **Ir a…** y tema |
| `wwwroot/css/site.css` | `@import mf-experience-system.css` |

---

## Design tokens (contrato)

- **Colores:** `--mf-bg`, `--mf-surface`, `--mf-border`, `--mf-text`, `--mf-muted`, `--mf-accent`, `--mf-accent-soft`.
- **Radios:** `--mf-radius-md`, `--mf-radius-lg`.
- **Sombras:** `--mf-shadow-sm`, `--mf-shadow-md`.
- **Tipografía:** Plus Jakarta Sans (Google Fonts en CSS layer); escala fluida `clamp()` ya preparada en `.mf-xp-*`.

**Regla:** nuevos componentes **solo** consumen tokens; no colores literales en vistas nuevas.

---

## Componentes requeridos (roadmap UI)

| Prioridad | Componente | Uso |
|-----------|------------|-----|
| P0 | Command palette (listo v0) | Navegación power-user |
| P0 | Dark mode (listo v0) | Preferencia + prestigio |
| P1 | Skeleton global en listas/cards | Realtime feeling |
| P1 | Empty states ilustrados | Retención emocional |
| P2 | Micro-motion (150–220ms, `prefers-reduced-motion`) | Pulido |
| P2 | Glass sutil en headers modales | Premium sin ruido |
| P3 | Data viz unificado (Chart.js theme tokens) | Dashboard CEO |

---

## Rollout por pantalla

1. **Shell** (navbar, layout, paleta) — **hecho v0**
2. **Dashboard** — aplicar `.mf-xp-card` / `.mf-xp-kpi` al índice principal
3. **Listas** — densidad “comfortable”, menos tabla plana; chips de estado
4. **Formularios** — agrupación visual, validación inline, progreso en wizards

---

## Accesibilidad

- Paleta: foco visible, Escape cierra, Enter ejecuta, flechas navegan.
- Contraste AA mínimo en dark/light (ajustar `--mf-muted` si QA falla).

---

## No hacer

- Duplicar Bootstrap con otro framework sin migración planificada.
- Romper responsive existente; mobile-first en nuevos bloques.

---

*Actualizar cuando el Dashboard CEO adopte el mismo sistema visual.*
