using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public partial class AddNotificationWebPushOutbox : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            name: "NotificationWebPushOutbox",
            columns: table => new {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                NextAttemptOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                AttemptCount = table.Column<int>(type: "integer", nullable: false),
                ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
            },
            constraints: table => {
                table.PrimaryKey("PK_NotificationWebPushOutbox", x => x.Id);
                table.ForeignKey(
                    name: "FK_NotificationWebPushOutbox_Notifications_NotificationId",
                    column: x => x.NotificationId,
                    principalTable: "Notifications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_NotificationWebPushOutbox_NotificationId",
            table: "NotificationWebPushOutbox",
            column: "NotificationId");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationWebPushOutbox_ProcessedOnUtc_NextAttemptOnUtc",
            table: "NotificationWebPushOutbox",
            columns: ["ProcessedOnUtc", "NextAttemptOnUtc"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            name: "NotificationWebPushOutbox");
    }
}
