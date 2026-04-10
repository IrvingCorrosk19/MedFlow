SELECT u."Email", r."Name" AS role
FROM "AspNetUsers" u
JOIN "AspNetUserRoles" ur ON ur."UserId" = u."Id"
JOIN "AspNetRoles" r ON r."Id" = ur."RoleId"
WHERE u."Email" IN ('staff@medflow.ai','billing@medflow.ai')
ORDER BY u."Email", r."Name";
