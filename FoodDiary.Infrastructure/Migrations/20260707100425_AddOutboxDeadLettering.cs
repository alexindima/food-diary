using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public partial class AddOutboxDeadLettering : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropIndex(
            name: "IX_NotificationWebPushOutbox_DueLease",
            table: "NotificationWebPushOutbox");

        migrationBuilder.DropIndex(
            name: "IX_ImageObjectDeletionOutbox_DueLease",
            table: "ImageObjectDeletionOutbox");

        migrationBuilder.DropIndex(
            name: "IX_EmailOutbox_DueLease",
            table: "EmailOutbox");

        migrationBuilder.AddColumn<DateTime>(
            name: "DeadLetteredOnUtc",
            table: "NotificationWebPushOutbox",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "DeadLetteredOnUtc",
            table: "ImageObjectDeletionOutbox",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "DeadLetteredOnUtc",
            table: "EmailOutbox",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_NotificationWebPushOutbox_DueLease",
            table: "NotificationWebPushOutbox",
            columns: ["ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptOnUtc", "LockedUntilUtc"]);

        migrationBuilder.CreateIndex(
            name: "IX_ImageObjectDeletionOutbox_DueLease",
            table: "ImageObjectDeletionOutbox",
            columns: ["ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptOnUtc", "LockedUntilUtc"]);

        migrationBuilder.CreateIndex(
            name: "IX_EmailOutbox_DueLease",
            table: "EmailOutbox",
            columns: ["ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptOnUtc", "LockedUntilUtc"]);
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
            name: "DeadLetteredOnUtc",
            table: "NotificationWebPushOutbox");

        migrationBuilder.DropColumn(
            name: "DeadLetteredOnUtc",
            table: "ImageObjectDeletionOutbox");

        migrationBuilder.DropColumn(
            name: "DeadLetteredOnUtc",
            table: "EmailOutbox");

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
}
