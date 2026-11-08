using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddBillingCancellationTelegramConfirmation : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmationTelegramHandledAtUtc",
                table: "BillingCancellations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingCancellations_TelegramPending",
                table: "BillingCancellations",
                columns: ["ConfirmationTelegramHandledAtUtc", "RequestedAtUtc"],
                filter: "\"ConfirmationTelegramHandledAtUtc\" IS NULL AND \"Provider\" = 'TelegramStars'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_BillingCancellations_TelegramPending",
                table: "BillingCancellations");

            migrationBuilder.DropColumn(
                name: "ConfirmationTelegramHandledAtUtc",
                table: "BillingCancellations");
        }
    }
}
