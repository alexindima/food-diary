using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddBillingCancellationEmailDelivery : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmationEmailHandledAtUtc",
                table: "BillingCancellations",
                type: "timestamp with time zone",
                nullable: true);

            // Existing history predates confirmations; do not send retrospective cancellation emails.
            migrationBuilder.Sql("""
                UPDATE "BillingCancellations"
                SET "ConfirmationEmailHandledAtUtc" = "RequestedAtUtc"
                WHERE "ConfirmationEmailHandledAtUtc" IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_BillingCancellations_ConfirmationEmailHandledAtUtc_Requeste~",
                table: "BillingCancellations",
                columns: ["ConfirmationEmailHandledAtUtc", "RequestedAtUtc"],
                filter: "\"ConfirmationEmailHandledAtUtc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_BillingCancellations_ConfirmationEmailHandledAtUtc_Requeste~",
                table: "BillingCancellations");

            migrationBuilder.DropColumn(
                name: "ConfirmationEmailHandledAtUtc",
                table: "BillingCancellations");
        }
    }
}
