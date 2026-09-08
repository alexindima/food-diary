using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddEmailPurposeAndBugAcknowledgement : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.Sql("""
                INSERT INTO "EmailTemplates" ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc")
                SELECT '331bcfa0-d0e3-46c9-a732-4e88493fa9f0', 'bug_report_received', 'en', 'We received your bug report',
                    '<p>Thank you for helping improve {{brand}}!</p><p>We received your report and will look into it. If we need more details, we will reply in this conversation.</p><p>The {{brand}} team</p>',
                    'Thank you for helping improve {{brand}}! We received your report and will look into it. If we need more details, we will reply in this conversation. The {{brand}} team', true, NOW(), NULL
                WHERE NOT EXISTS (SELECT 1 FROM "EmailTemplates" WHERE "Key" = 'bug_report_received' AND "Locale" = 'en');
                INSERT INTO "EmailTemplates" ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc")
                SELECT '331bcfa0-d0e3-46c9-a732-4e88493fa9f1', 'bug_report_received', 'ru', 'Получили ваше сообщение об ошибке',
                    '<p>Спасибо, что помогаете улучшать {{brand}}!</p><p>Мы получили ваше сообщение и изучим его. Если понадобятся подробности, ответим в этой переписке.</p><p>Команда {{brand}}</p>',
                    'Спасибо, что помогаете улучшать {{brand}}! Мы получили ваше сообщение и изучим его. Если понадобятся подробности, ответим в этой переписке. Команда {{brand}}', true, NOW(), NULL
                WHERE NOT EXISTS (SELECT 1 FROM "EmailTemplates" WHERE "Key" = 'bug_report_received' AND "Locale" = 'ru');
                """);

            migrationBuilder.AddColumn<bool>(
                name: "AutoSubmitted",
                table: "EmailOutbox",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "EmailOutbox",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InReplyTo",
                table: "EmailOutbox",
                type: "character varying(998)",
                maxLength: 998,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "EmailOutbox",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "other");

            migrationBuilder.AddColumn<string>(
                name: "ReplyTo",
                table: "EmailOutbox",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropColumn(
                name: "AutoSubmitted",
                table: "EmailOutbox");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "EmailOutbox");

            migrationBuilder.DropColumn(
                name: "InReplyTo",
                table: "EmailOutbox");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "EmailOutbox");

            migrationBuilder.DropColumn(
                name: "ReplyTo",
                table: "EmailOutbox");
        }
    }
}
