using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public partial class AddPreviousRefreshTokenGrace : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<string>(
            name: "PreviousRefreshTokenHash",
            table: "UserRefreshTokenSessions",
            type: "character varying(512)",
            maxLength: 512,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "PreviousRefreshTokenValidUntilUtc",
            table: "UserRefreshTokenSessions",
            type: "timestamp with time zone",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            name: "PreviousRefreshTokenHash",
            table: "UserRefreshTokenSessions");

        migrationBuilder.DropColumn(
            name: "PreviousRefreshTokenValidUntilUtc",
            table: "UserRefreshTokenSessions");
    }
}
