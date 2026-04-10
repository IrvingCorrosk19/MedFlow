-- Listado de usuarios con roles (sin contraseñas)
SELECT
  u."Email"             AS email,
  u."UserName"          AS username,
  u."EmailConfirmed"    AS email_confirmed,
  u."LockoutEnd"        AS lockout_end,
  u."TenantId"          AS tenant_id,
  COALESCE(string_agg(r."Name", ', ' ORDER BY r."Name"), '') AS roles
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON ur."UserId" = u."Id"
LEFT JOIN "AspNetRoles" r      ON r."Id"      = ur."RoleId"
GROUP BY u."Id", u."Email", u."UserName", u."EmailConfirmed", u."LockoutEnd", u."TenantId"
ORDER BY u."Email";

-- Listado de roles existentes
SELECT r."Name" AS role
FROM "AspNetRoles" r
ORDER BY r."Name";
