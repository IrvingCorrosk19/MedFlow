-- Suscripción y plan actual por código de tenant (ajustar el código si hace falta).
SELECT t."Id", t."Code", t."Name", t."CurrentSubscriptionId", t."CommercialStatus", t."IsSuspended"
FROM "Tenants" t
WHERE t."Code" IN ('clinica-aurora-qa-4', 'demo')
  AND NOT t."IsDeleted"
ORDER BY t."Code";

SELECT ts."TenantId", ts."Id", sp."Name" AS plan_name, ts."Status", ts."ExternalSubscriptionId"
FROM "TenantSubscriptions" ts
JOIN "SubscriptionPlans" sp ON sp."Id" = ts."SubscriptionPlanId"
WHERE ts."TenantId" IN (
  SELECT t."Id" FROM "Tenants" t WHERE t."Code" IN ('clinica-aurora-qa-4', 'demo') AND NOT t."IsDeleted"
)
AND NOT ts."IsDeleted"
ORDER BY ts."CreatedAt" DESC;
