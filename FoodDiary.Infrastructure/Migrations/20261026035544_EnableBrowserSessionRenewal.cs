using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class EnableBrowserSessionRenewal : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<bool>(
                name: "BrowserSlidingExpirationEnabled",
                table: "UserRefreshTokenSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE "UserRefreshTokenSessions"
                SET "BrowserSlidingExpirationEnabled" = TRUE
                WHERE "BrowserSecurityVersion" IS NOT NULL
                  AND "RevokedAtUtc" IS NULL AND "ExpiresAtUtc" > CURRENT_TIMESTAMP
                  AND "AuthProvider" IN ('password', 'password-register', 'password-restore', 'password-reset',
                                         'google', 'telegram', 'telegram-mini-app', 'telegram-login-widget');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropColumn(
                name: "BrowserSlidingExpirationEnabled",
                table: "UserRefreshTokenSessions");
        }
    }
}
