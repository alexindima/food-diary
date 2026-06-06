using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class EnforceUniqueBillingPayments : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_BillingPayments_Provider_ExternalPaymentId",
                table: "BillingPayments");

            migrationBuilder.Sql(
                """
                DELETE FROM "BillingPayments" duplicate
                USING "BillingPayments" kept
                WHERE duplicate."Provider" = kept."Provider"
                    AND duplicate."ExternalPaymentId" = kept."ExternalPaymentId"
                    AND (
                        duplicate."CreatedOnUtc" > kept."CreatedOnUtc"
                        OR (
                            duplicate."CreatedOnUtc" = kept."CreatedOnUtc"
                            AND duplicate."Id" > kept."Id"
                        )
                    );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_Provider_ExternalPaymentId",
                table: "BillingPayments",
                columns: ["Provider", "ExternalPaymentId"],
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_BillingPayments_Provider_ExternalPaymentId",
                table: "BillingPayments");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_Provider_ExternalPaymentId",
                table: "BillingPayments",
                columns: ["Provider", "ExternalPaymentId"]);
        }
    }
}
