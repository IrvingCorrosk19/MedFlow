SELECT t."Code", t."CommercialStatus", ts."Status" AS sub_status
FROM "Tenants" t
JOIN "TenantSubscriptions" ts ON ts."Id" = t."CurrentSubscriptionId"
WHERE t."Code" = 'demo';
