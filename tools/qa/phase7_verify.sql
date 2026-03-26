SELECT ts."TenantId", ts."Id" AS subscription_row_id, ts."ExternalSubscriptionId"
FROM "TenantSubscriptions" ts
WHERE ts."ExternalSubscriptionId" = 'sub_qa_phase7_medflow' AND NOT ts."IsDeleted";

SELECT COUNT(*) AS prefs_ready FROM "NotificationPreferences" np
WHERE np."EventType" = 5 AND np."Channel" = 0 AND np."IsEnabled" AND NOT np."IsDeleted";
