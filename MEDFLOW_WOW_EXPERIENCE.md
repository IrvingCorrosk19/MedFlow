# MedFlow WOW Experience

**Definición:** La sensación de que el producto **anticipa**, **reduce fricción**, y **celebra progreso** — sin ruido visual.

---

## Momentos WOW (diseño intencional)

| Momento | Usuario | WOW |
|---------|---------|-----|
| Primer login | Admin | Onboarding guiado + “tu clínica ya tiene datos demo opcionales” |
| Abrir app diaria | Staff | Command palette **⌘K** — ya disponible v0 |
| Ver dashboard | Director | Un insight IA arriba del fold: “Perdiste $X por no-shows” |
| Pagar / reagendar | Paciente | Un tap, confirmación clara, sin formularios infinitos |
| Cobrar | Finanzas | “Recuperado esta semana: $Y” atribuido a campaña |

---

## Micro-interacciones (reglas)

- Duración **150–220 ms**; respetar `prefers-reduced-motion`.
- Feedback en **botones** y **toasts** consistentes con tokens `--mf-*`.
- Skeleton **antes** de spinners genéricos en listas nuevas.

---

## Anti-patrones (NO)

- Tablas densas sin jerarquía visual.
- Modales encadenados sin progreso.
- Mensajes de error técnicos al paciente.

---

## Medición

- **Task success rate** en flujos top (agenda, pago, portal).
- **Time on task** antes/después de aplicar `.mf-xp-*`.
- **Qualitative:** 5 entrevistas trimestrales con clínicas piloto.

---

*El WOW es función + emoción; no reemplaza KPIs de negocio.*
