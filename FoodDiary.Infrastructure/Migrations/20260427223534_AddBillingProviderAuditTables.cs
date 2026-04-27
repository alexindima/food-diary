using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class AddBillingProviderAuditTables : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<string>(
                name: "ExternalPaymentMethodId",
                table: "BillingSubscriptions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextBillingAttemptUtc",
                table: "BillingSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderMetadataJson",
                table: "BillingSubscriptions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BillingPayments",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingSubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalPaymentId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExternalCustomerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExternalSubscriptionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExternalPaymentMethodId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExternalPriceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Plan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    CurrentPeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentPeriodEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WebhookEventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ProviderMetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_BillingPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingPayments_BillingSubscriptions_BillingSubscriptionId",
                        column: x => x.BillingSubscriptionId,
                        principalTable: "BillingSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BillingPayments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillingWebhookEvents",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExternalObjectId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_BillingWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingSubscriptions_Provider_ExternalPaymentMethodId",
                table: "BillingSubscriptions",
                columns: new[] { "Provider", "ExternalPaymentMethodId" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_BillingSubscriptionId",
                table: "BillingPayments",
                column: "BillingSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_Provider_ExternalCustomerId",
                table: "BillingPayments",
                columns: new[] { "Provider", "ExternalCustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_Provider_ExternalPaymentId",
                table: "BillingPayments",
                columns: new[] { "Provider", "ExternalPaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_Provider_ExternalPaymentMethodId",
                table: "BillingPayments",
                columns: new[] { "Provider", "ExternalPaymentMethodId" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_Provider_ExternalSubscriptionId",
                table: "BillingPayments",
                columns: new[] { "Provider", "ExternalSubscriptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_UserId",
                table: "BillingPayments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingWebhookEvents_ProcessedAtUtc",
                table: "BillingWebhookEvents",
                column: "ProcessedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BillingWebhookEvents_Provider_EventId",
                table: "BillingWebhookEvents",
                columns: new[] { "Provider", "EventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "BillingPayments");

            migrationBuilder.DropTable(
                name: "BillingWebhookEvents");

            migrationBuilder.DropIndex(
                name: "IX_BillingSubscriptions_Provider_ExternalPaymentMethodId",
                table: "BillingSubscriptions");

            migrationBuilder.DropColumn(
                name: "ExternalPaymentMethodId",
                table: "BillingSubscriptions");

            migrationBuilder.DropColumn(
                name: "NextBillingAttemptUtc",
                table: "BillingSubscriptions");

            migrationBuilder.DropColumn(
                name: "ProviderMetadataJson",
                table: "BillingSubscriptions");
        }
    }
}
