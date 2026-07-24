using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddClientTaskDueReminders : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueReminderSentAtUtc",
                table: "ClientTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientTasks_Status_DueAtUtc_DueReminderSentAtUtc",
                table: "ClientTasks",
                columns: ["Status", "DueAtUtc", "DueReminderSentAtUtc"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_ClientTasks_Status_DueAtUtc_DueReminderSentAtUtc",
                table: "ClientTasks");

            migrationBuilder.DropColumn(
                name: "DueReminderSentAtUtc",
                table: "ClientTasks");
        }
    }
}
