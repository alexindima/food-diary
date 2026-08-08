using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public partial class AddBillingWebhookInboxAndPaymentOccurrence : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<DateTime>(
            name: "OccurredAtUtc",
            table: "BillingPayments",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.Sql(
            "CREATE INDEX \"IX_BillingPayments_EffectiveOccurredAtUtc\" ON \"BillingPayments\" (COALESCE(\"OccurredAtUtc\", \"CreatedOnUtc\")) WHERE \"Amount\" IS NOT NULL AND \"Currency\" IS NOT NULL");

        migrationBuilder.AlterColumn<DateTime>(
            name: "ProcessedAtUtc",
            table: "BillingWebhookEvents",
            type: "timestamp with time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.AddColumn<int>(
            name: "AttemptCount",
            table: "BillingWebhookEvents",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<DateTime>(
            name: "NextAttemptAtUtc",
            table: "BillingWebhookEvents",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ParsedEventJson",
            table: "BillingWebhookEvents",
            type: "jsonb",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ReceivedAtUtc",
            table: "BillingWebhookEvents",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.Sql(
            "UPDATE \"BillingWebhookEvents\" SET \"ReceivedAtUtc\" = COALESCE(\"ProcessedAtUtc\", \"CreatedOnUtc\") WHERE \"ReceivedAtUtc\" IS NULL");

        migrationBuilder.AlterColumn<DateTime>(
            name: "ReceivedAtUtc",
            table: "BillingWebhookEvents",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_BillingWebhookEvents_Status_NextAttemptAtUtc_ReceivedAtUtc",
            table: "BillingWebhookEvents",
            columns: ["Status", "NextAttemptAtUtc", "ReceivedAtUtc"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_BillingPayments_EffectiveOccurredAtUtc\"");

        migrationBuilder.DropIndex(
            name: "IX_BillingWebhookEvents_Status_NextAttemptAtUtc_ReceivedAtUtc",
            table: "BillingWebhookEvents");

        migrationBuilder.DropColumn(name: "AttemptCount", table: "BillingWebhookEvents");
        migrationBuilder.DropColumn(name: "NextAttemptAtUtc", table: "BillingWebhookEvents");
        migrationBuilder.DropColumn(name: "ParsedEventJson", table: "BillingWebhookEvents");

        migrationBuilder.Sql(
            "UPDATE \"BillingWebhookEvents\" SET \"ProcessedAtUtc\" = \"ReceivedAtUtc\" WHERE \"ProcessedAtUtc\" IS NULL");

        migrationBuilder.DropColumn(name: "ReceivedAtUtc", table: "BillingWebhookEvents");

        migrationBuilder.AlterColumn<DateTime>(
            name: "ProcessedAtUtc",
            table: "BillingWebhookEvents",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: default(DateTime),
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldNullable: true);

        migrationBuilder.DropColumn(name: "OccurredAtUtc", table: "BillingPayments");
    }
}
