using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddPrivacyConsentHistory : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.Sql("UPDATE \"BillingWebhookEvents\" SET \"PayloadJson\" = NULL, \"ParsedEventJson\" = NULL WHERE \"Status\" = 'processed'");
            migrationBuilder.AddColumn<string>(
                name: "AiConsentNoticeVersion",
                table: "Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HealthConsentAcceptedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HealthConsentDeclinedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HealthConsentNoticeVersion",
                table: "Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MarketingConsentAcceptedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarketingConsentNoticeVersion",
                table: "Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentGrantedAtUtc",
                table: "MarketingAttributionEvents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsentNoticeVersion",
                table: "MarketingAttributionEvents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserPrivacyConsentEvents",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Granted = table.Column<bool>(type: "boolean", nullable: false),
                    NoticeVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_UserPrivacyConsentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPrivacyConsentEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPrivacyConsentEvents_UserId_OccurredAtUtc",
                table: "UserPrivacyConsentEvents",
                columns: ["UserId", "OccurredAtUtc"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "UserPrivacyConsentEvents");

            migrationBuilder.DropColumn(
                name: "AiConsentNoticeVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HealthConsentAcceptedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HealthConsentDeclinedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HealthConsentNoticeVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MarketingConsentAcceptedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MarketingConsentNoticeVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ConsentGrantedAtUtc",
                table: "MarketingAttributionEvents");

            migrationBuilder.DropColumn(
                name: "ConsentNoticeVersion",
                table: "MarketingAttributionEvents");
        }
    }
}
