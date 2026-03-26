DO $$
BEGIN
  UPDATE "TenantSubscriptions"
  SET "Status" = 1,
      "LastBillingSyncAt" = now()
  WHERE "Id" = 'aaee3108-855e-44b0-bbf7-df8eac4cbdae'::uuid;

  UPDATE "Tenants"
  SET "CommercialStatus" = 0,
      "IsSuspended" = false,
      "SuspensionReason" = NULL,
      "SuspendedAt" = NULL
  WHERE "Id" = '47602bdf-4750-4796-afbc-02c8bdaf4613'::uuid;
END $$;
