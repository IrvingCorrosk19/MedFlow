# Bugs encontrados y corrección

## 1. Acceso denegado (paciente) mostraba UI completa del staff (CRÍTICO UX/seguridad)

- **Síntoma:** Con sesión de paciente (`Patient`), navegar a `/AdminUsers` mostraba `Account/AccessDenied` embebido en `_AdminLayout` con sidebar (Dashboard, Usuarios, Seguridad, etc.).
- **Causa:** `Views/Account/AccessDenied.cshtml` usaba siempre `_AdminLayout`.
- **Corrección:**
  - Nuevo `Views/Shared/_AccessDeniedMinimalLayout.cshtml` (sin sidebar).
  - `AccessDenied.cshtml`: si el usuario es `Patient` y no tiene rol de staff (Admin, Reception, Doctor, Billing, Staff), layout mínimo y botón a `/PatientPortal/inicio`.
- **Verificación:** HTML sin `sidebar`; enlace “Volver al portal del paciente”; snapshot navegador solo 3 elementos interactivos (mensaje + enlace).
- **Build:** `dotnet build src/MedFlow.Web/MedFlow.Web.csproj` OK (tras detener proceso que bloqueaba DLL).
- **Tests:** `MedFlow.UnitTests` 112 passed.

## 2. Automatización HTTP

- **Síntoma:** `Invoke-WebRequest` con hashtable en POST no autenticaba (login devolvía 200 con HTML de login).
- **Causa:** binding de formulario; se requiere `application/x-www-form-urlencoded` explícito.
- **Mitigación en pruebas:** body url-encoded con `Email`, `Password`, `RememberMe`, `__RequestVerificationToken`.

No se detectaron otros fallos bloqueantes en la matriz HTTP documentada.
