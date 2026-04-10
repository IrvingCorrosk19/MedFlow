SELECT
  u."Email",
  u."LockoutEnd",
  u."AccessFailedCount",
  u."EmailConfirmed",
  u."TenantId",
  u."IsActive",
  u."IsLocked",
  left(u."PasswordHash", 25) AS hash_prefix
FROM "AspNetUsers" u
WHERE u."Email" = 'billing@medflow.ai';
