using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public partial class RenameLegacyFastingProtocolValues : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        RenameProtocols(
            migrationBuilder,
            "F16_8", "Fast16Eat8",
            "F18_6", "Fast18Eat6",
            "F20_4", "Fast20Eat4",
            "F24_0", "Fast24",
            "F36_0", "Fast36",
            "F72_0", "Fast72");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        RenameProtocols(
            migrationBuilder,
            "Fast16Eat8", "F16_8",
            "Fast18Eat6", "F18_6",
            "Fast20Eat4", "F20_4",
            "Fast24", "F24_0",
            "Fast36", "F36_0",
            "Fast72", "F72_0");
    }

    private static void RenameProtocols(
        MigrationBuilder migrationBuilder,
        string from16,
        string to16,
        string from18,
        string to18,
        string from20,
        string to20,
        string from24,
        string to24,
        string from36,
        string to36,
        string from72,
        string to72) {
        foreach (string table in new[] { "FastingSessions", "FastingPlans", "FastingTelemetryEvents" }) {
            migrationBuilder.Sql($$"""
                UPDATE "{{table}}"
                SET "Protocol" = CASE "Protocol"
                    WHEN '{{from16}}' THEN '{{to16}}'
                    WHEN '{{from18}}' THEN '{{to18}}'
                    WHEN '{{from20}}' THEN '{{to20}}'
                    WHEN '{{from24}}' THEN '{{to24}}'
                    WHEN '{{from36}}' THEN '{{to36}}'
                    WHEN '{{from72}}' THEN '{{to72}}'
                    ELSE "Protocol"
                END
                WHERE "Protocol" IN ('{{from16}}', '{{from18}}', '{{from20}}', '{{from24}}', '{{from36}}', '{{from72}}');
                """);
        }
    }
}
