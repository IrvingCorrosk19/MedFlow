using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaaSBillingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalPlanId",
                table: "TenantBillingProfiles");

            migrationBuilder.DropColumn(
                name: "ExternalSubscriptionId",
                table: "TenantBillingProfiles");

            migrationBuilder.AddColumn<bool>(
                name: "AutoRenew",
                table: "TenantSubscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BillingPeriod",
                table: "TenantSubscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BillingProvider",
                table: "TenantSubscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "CancelAtPeriodEnd",
                table: "TenantSubscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentPeriodEnd",
                table: "TenantSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentPeriodStart",
                table: "TenantSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalPriceId",
                table: "TenantSubscriptions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalProductId",
                table: "TenantSubscriptions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastBillingSyncAt",
                table: "TenantSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                table: "TenantBillingProfiles",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "TenantBillingProfiles",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "TenantBillingProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "TenantBillingProfiles",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "TenantBillingProfiles",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "TenantBillingProfiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredCurrency",
                table: "TenantBillingProfiles",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateProvince",
                table: "TenantBillingProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "TenantBillingProfiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePriceIdAnnual",
                table: "SubscriptionPlans",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePriceIdMonthly",
                table: "SubscriptionPlans",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeProductId",
                table: "SubscriptionPlans",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SaaSBillingTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantSubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ProviderInvoiceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ProviderPaymentIntentId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaaSBillingTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaaSBillingTransactions_TenantSubscriptions_TenantSubscript~",
                        column: x => x.TenantSubscriptionId,
                        principalTable: "TenantSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SaaSBillingTransactions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaaSInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantSubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BillingPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BillingPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProviderInvoiceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    InvoiceUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    PdfUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaaSInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaaSInvoices_TenantSubscriptions_TenantSubscriptionId",
                        column: x => x.TenantSubscriptionId,
                        principalTable: "TenantSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SaaSInvoices_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StripeWebhookEventLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderEventId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeWebhookEventLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantBillingProfiles_ExternalCustomerId",
                table: "TenantBillingProfiles",
                column: "ExternalCustomerId",
                filter: "\"ExternalCustomerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SaaSBillingTransactions_OccurredAt",
                table: "SaaSBillingTransactions",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_SaaSBillingTransactions_ProviderTransactionId",
                table: "SaaSBillingTransactions",
                column: "ProviderTransactionId",
                filter: "\"ProviderTransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SaaSBillingTransactions_TenantId",
                table: "SaaSBillingTransactions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SaaSBillingTransactions_TenantSubscriptionId",
                table: "SaaSBillingTransactions",
                column: "TenantSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SaaSInvoices_InvoiceNumber",
                table: "SaaSInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaaSInvoices_ProviderInvoiceId",
                table: "SaaSInvoices",
                column: "ProviderInvoiceId",
                filter: "\"ProviderInvoiceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SaaSInvoices_TenantId",
                table: "SaaSInvoices",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SaaSInvoices_TenantSubscriptionId",
                table: "SaaSInvoices",
                column: "TenantSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_StripeWebhookEventLogs_EventType",
                table: "StripeWebhookEventLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_StripeWebhookEventLogs_ProcessedAt",
                table: "StripeWebhookEventLogs",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StripeWebhookEventLogs_ProviderEventId",
                table: "StripeWebhookEventLogs",
                column: "ProviderEventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SaaSBillingTransactions");

            migrationBuilder.DropTable(
                name: "SaaSInvoices");

            migrationBuilder.DropTable(
                name: "StripeWebhookEventLogs");

            migrationBuilder.DropIndex(
                name: "IX_TenantBillingProfiles_ExternalCustomerId",
                table: "TenantBillingProfiles");

            migrationBuilder.DropColumn(
                name: "AutoRenew",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "BillingPeriod",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "BillingProvider",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "CancelAtPeriodEnd",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "CurrentPeriodEnd",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "CurrentPeriodStart",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "ExternalPriceId",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "ExternalProductId",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "LastBillingSyncAt",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "AddressLine1",
                table: "TenantBillingProfiles");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "TenantBillingProfiles");

            migrationBuilder.DropColumn(
                name: "City",
                table: "TenantBillingProfiles");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "TenantBillingProfiles");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "TenantBillingProfiles");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "TenantBillingProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredCurrency",
                table: "TenantBillingProfiles");

            migrationBuilder.DropColumn(
                name: "StateProvince",
                table: "TenantBillingProfiles");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "TenantBillingProfiles");

            migrationBuilder.DropColumn(
                name: "StripePriceIdAnnual",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "StripePriceIdMonthly",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "StripeProductId",
                table: "SubscriptionPlans");

            migrationBuilder.AddColumn<string>(
                name: "ExternalPlanId",
                table: "TenantBillingProfiles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSubscriptionId",
                table: "TenantBillingProfiles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }
    }
}
