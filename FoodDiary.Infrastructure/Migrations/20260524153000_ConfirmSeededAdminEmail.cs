using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public partial class ConfirmSeededAdminEmail : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql("""
            UPDATE "Users"
            SET "IsEmailConfirmed" = TRUE
            WHERE "Email" = 'admin@fooddiary.club';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql("""
            UPDATE "Users"
            SET "IsEmailConfirmed" = FALSE
            WHERE "Email" = 'admin@fooddiary.club';
            """);
    }
}
