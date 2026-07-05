using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public partial class AddOutboxMessageLeases : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropIndex(
            name: "IX_NotificationWebPushOutbox_ProcessedOnUtc_NextAttemptOnUtc",
            table: "NotificationWebPushOutbox");

        migrationBuilder.DropIndex(
            name: "IX_ImageObjectDeletionOutbox_ProcessedOnUtc_NextAttemptOnUtc",
            table: "ImageObjectDeletionOutbox");

        migrationBuilder.DropIndex(
            name: "IX_EmailOutbox_ProcessedOnUtc_NextAttemptOnUtc",
            table: "EmailOutbox");

        migrationBuilder.AddColumn<string>(
            name: "LockedBy",
            table: "NotificationWebPushOutbox",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LockedUntilUtc",
            table: "NotificationWebPushOutbox",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LockedBy",
            table: "ImageObjectDeletionOutbox",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LockedUntilUtc",
            table: "ImageObjectDeletionOutbox",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LockedBy",
            table: "EmailOutbox",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LockedUntilUtc",
            table: "EmailOutbox",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_NotificationWebPushOutbox_DueLease",
            table: "NotificationWebPushOutbox",
            columns: ["ProcessedOnUtc", "NextAttemptOnUtc", "LockedUntilUtc"]);

        migrationBuilder.CreateIndex(
            name: "IX_ImageObjectDeletionOutbox_DueLease",
            table: "ImageObjectDeletionOutbox",
            columns: ["ProcessedOnUtc", "NextAttemptOnUtc", "LockedUntilUtc"]);

        migrationBuilder.CreateIndex(
            name: "IX_EmailOutbox_DueLease",
            table: "EmailOutbox",
            columns: ["ProcessedOnUtc", "NextAttemptOnUtc", "LockedUntilUtc"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropIndex(
            name: "IX_NotificationWebPushOutbox_DueLease",
            table: "NotificationWebPushOutbox");

        migrationBuilder.DropIndex(
            name: "IX_ImageObjectDeletionOutbox_DueLease",
            table: "ImageObjectDeletionOutbox");

        migrationBuilder.DropIndex(
            name: "IX_EmailOutbox_DueLease",
            table: "EmailOutbox");

        migrationBuilder.DropColumn(
            name: "LockedBy",
            table: "NotificationWebPushOutbox");

        migrationBuilder.DropColumn(
            name: "LockedUntilUtc",
            table: "NotificationWebPushOutbox");

        migrationBuilder.DropColumn(
            name: "LockedBy",
            table: "ImageObjectDeletionOutbox");

        migrationBuilder.DropColumn(
            name: "LockedUntilUtc",
            table: "ImageObjectDeletionOutbox");

        migrationBuilder.DropColumn(
            name: "LockedBy",
            table: "EmailOutbox");

        migrationBuilder.DropColumn(
            name: "LockedUntilUtc",
            table: "EmailOutbox");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationWebPushOutbox_ProcessedOnUtc_NextAttemptOnUtc",
            table: "NotificationWebPushOutbox",
            columns: ["ProcessedOnUtc", "NextAttemptOnUtc"]);

        migrationBuilder.CreateIndex(
            name: "IX_ImageObjectDeletionOutbox_ProcessedOnUtc_NextAttemptOnUtc",
            table: "ImageObjectDeletionOutbox",
            columns: ["ProcessedOnUtc", "NextAttemptOnUtc"]);

        migrationBuilder.CreateIndex(
            name: "IX_EmailOutbox_ProcessedOnUtc_NextAttemptOnUtc",
            table: "EmailOutbox",
            columns: ["ProcessedOnUtc", "NextAttemptOnUtc"]);
    }
}
