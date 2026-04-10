SELECT count(*) AS cnt
FROM "AspNetUsers"
WHERE "Email" = 'billing@medflow.ai';

SELECT "Id", "Email", "UserName", "LockoutEnabled", "LockoutEnd", "AccessFailedCount"
FROM "AspNetUsers"
WHERE "Email" = 'billing@medflow.ai';
