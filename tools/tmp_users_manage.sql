SELECT p."Id", p."Code", p."IsDeleted"
FROM "Permissions" p
WHERE p."Code" = 'users.manage';

SELECT r."Name" AS role, p."Code"
FROM "RolePermissions" rp
JOIN "AspNetRoles" r ON r."Id" = rp."RoleId"
JOIN "Permissions" p ON p."Id" = rp."PermissionId"
WHERE p."Code" = 'users.manage'
ORDER BY r."Name";
