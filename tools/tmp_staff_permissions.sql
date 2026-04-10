-- Permisos asignados a Staff
SELECT p."Code"
FROM "AspNetRoles" r
JOIN "RolePermissions" rp ON rp."RoleId" = r."Id"
JOIN "Permissions" p ON p."Id" = rp."PermissionId"
WHERE r."Name" = 'Staff'
ORDER BY p."Code";

-- Permisos asignados a Reception
SELECT p."Code"
FROM "AspNetRoles" r
JOIN "RolePermissions" rp ON rp."RoleId" = r."Id"
JOIN "Permissions" p ON p."Id" = rp."PermissionId"
WHERE r."Name" = 'Reception'
ORDER BY p."Code";
