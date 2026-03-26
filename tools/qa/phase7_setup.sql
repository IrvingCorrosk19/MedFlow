-- Phase 7 QA: tenant con suscripción, ExternalSubscriptionId fijo, plantilla y preferencia para SubscriptionPaymentFailed (5) + Email (0).
DO $$
DECLARE
  tid uuid;
  subid uuid;
  tplid uuid;
BEGIN
  SELECT t."Id" INTO tid FROM "Tenants" t WHERE t."Code" = 'demo' AND NOT t."IsDeleted" LIMIT 1;
  IF tid IS NULL THEN
    SELECT t."Id" INTO tid FROM "Tenants" t WHERE NOT t."IsDeleted" ORDER BY t."CreatedAt" LIMIT 1;
  END IF;
  IF tid IS NULL THEN
    RAISE EXCEPTION 'No tenant found';
  END IF;
  RAISE NOTICE 'tenant_id=%', tid;

  SELECT ts."Id" INTO subid FROM "TenantSubscriptions" ts
    WHERE ts."TenantId" = tid AND NOT ts."IsDeleted"
    ORDER BY ts."CreatedAt" DESC LIMIT 1;
  IF subid IS NULL THEN
    RAISE EXCEPTION 'No subscription for tenant %', tid;
  END IF;

  UPDATE "TenantSubscriptions"
  SET "ExternalSubscriptionId" = 'sub_qa_phase7_medflow'
  WHERE "Id" = subid;

  SELECT nt."Id" INTO tplid FROM "NotificationTemplates" nt
    WHERE nt."TenantId" = tid AND nt."EventType" = 5 AND nt."Channel" = 0 AND NOT nt."IsDeleted"
    LIMIT 1;

  IF tplid IS NULL THEN
    tplid := gen_random_uuid();
    INSERT INTO "NotificationTemplates" (
      "Id", "TenantId", "EventType", "Channel", "Code", "Name",
      "SubjectTemplate", "BodyTemplate", "HtmlBodyTemplate",
      "IsDefault", "CreatedAt", "IsActive", "IsDeleted"
    ) VALUES (
      tplid, tid, 5, 0, 'sub_payment_failed_qa', 'QA Subscription Payment Failed',
      'Pago fallido: {{PlanName}}',
      'Monto {{Amount}} {{Currency}}. Razón: {{Reason}}',
      '<p>Monto {{Amount}} {{Currency}}. Razón: {{Reason}}</p>',
      true, now(), true, false
    );
  END IF;

  IF NOT EXISTS (
    SELECT 1 FROM "NotificationPreferences" np
    WHERE np."TenantId" = tid AND np."EventType" = 5 AND np."Channel" = 0 AND NOT np."IsDeleted"
  ) THEN
    INSERT INTO "NotificationPreferences" (
      "Id", "TenantId", "EventType", "Channel", "IsEnabled", "TemplateId",
      "CreatedAt", "IsActive", "IsDeleted"
    ) VALUES (
      gen_random_uuid(), tid, 5, 0, true, tplid,
      now(), true, false
    );
  END IF;
END $$;
