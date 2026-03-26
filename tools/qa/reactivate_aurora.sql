DO $$
DECLARE
  tid uuid := '067d82cf-f640-4a11-bf20-c19526b5919d'::uuid;
BEGIN
  UPDATE "Tenants"
  SET "IsSuspended" = false,
      "SuspensionReason" = NULL,
      "SuspendedAt" = NULL,
      "CommercialStatus" = 0
  WHERE "Id" = tid;

  UPDATE "TenantSubscriptions"
  SET "Status" = 1
  WHERE "TenantId" = tid AND "Id" = (
    SELECT "CurrentSubscriptionId" FROM "Tenants" WHERE "Id" = tid
  );
END $$;
