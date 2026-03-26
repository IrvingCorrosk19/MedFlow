SELECT "Id", "TenantId", "EventType", "Channel", "Status", "Recipient", "Subject", "ErrorMessage", "CreatedAt"
FROM "NotificationMessages"
WHERE "TenantId" = '47602bdf-4750-4796-afbc-02c8bdaf4613'::uuid
  AND "EventType" = 5
  AND NOT "IsDeleted"
ORDER BY "CreatedAt" DESC
LIMIT 5;

SELECT "ProviderEventId", "EventType", "IsProcessed", "ErrorMessage"
FROM "StripeWebhookEventLogs"
WHERE "ProviderEventId" = 'evt_qa_phase7_20260325_001'
  AND NOT "IsDeleted";
