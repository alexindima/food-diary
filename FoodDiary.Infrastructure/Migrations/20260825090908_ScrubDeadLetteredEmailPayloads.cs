using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public partial class ScrubDeadLetteredEmailPayloads : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql(
            """
            UPDATE "EmailOutbox"
            SET "ToAddressesJson" = '[]',
                "Subject" = '',
                "HtmlBody" = '',
                "TextBody" = NULL
            WHERE "DeadLetteredOnUtc" IS NOT NULL
              AND "ProcessedOnUtc" IS NULL;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        // Scrubbed sensitive payloads cannot be restored safely.
    }
}
