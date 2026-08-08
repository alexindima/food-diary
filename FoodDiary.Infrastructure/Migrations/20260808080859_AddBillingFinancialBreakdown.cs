using Microsoft.EntityFrameworkCore.Migrations;

using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddBillingFinancialBreakdown : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<decimal>(
                name: "Earnings",
                table: "BillingPayments",
                type: "numeric(18,2)",
                nullable: true,
                precision: 18,
                scale: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "Fee",
                table: "BillingPayments",
                type: "numeric(18,2)",
                nullable: true,
                precision: 18,
                scale: 2);

            migrationBuilder.AddColumn<string>(
                name: "PayoutCurrency",
                table: "BillingPayments",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PayoutEarnings",
                table: "BillingPayments",
                type: "numeric(18,2)",
                nullable: true,
                precision: 18,
                scale: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "Tax",
                table: "BillingPayments",
                type: "numeric(18,2)",
                nullable: true,
                precision: 18,
                scale: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropColumn(
                name: "Earnings",
                table: "BillingPayments");

            migrationBuilder.DropColumn(
                name: "Fee",
                table: "BillingPayments");

            migrationBuilder.DropColumn(
                name: "PayoutCurrency",
                table: "BillingPayments");

            migrationBuilder.DropColumn(
                name: "PayoutEarnings",
                table: "BillingPayments");

            migrationBuilder.DropColumn(
                name: "Tax",
                table: "BillingPayments");
        }
    }
}
