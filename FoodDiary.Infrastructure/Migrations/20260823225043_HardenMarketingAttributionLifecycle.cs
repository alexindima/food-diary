using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public partial class HardenMarketingAttributionLifecycle : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql("""
            DELETE FROM "MarketingAttributionEvents" duplicate
            USING "MarketingAttributionEvents" keeper
            WHERE duplicate."UserId" = keeper."UserId"
              AND duplicate."EventType" = keeper."EventType"
              AND duplicate."EventType" IN ('signup_completed', 'premium_started')
              AND (duplicate."OccurredAtUtc", duplicate."Id") > (keeper."OccurredAtUtc", keeper."Id");
            """);

        migrationBuilder.CreateIndex(
            name: "IX_MarketingAttributionEvents_UserId_EventType",
            table: "MarketingAttributionEvents",
            columns: ["UserId", "EventType"],
            unique: true,
            filter: "\"UserId\" IS NOT NULL AND \"EventType\" IN ('signup_completed', 'premium_started')");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropIndex(
            name: "IX_MarketingAttributionEvents_UserId_EventType",
            table: "MarketingAttributionEvents");
    }
}
