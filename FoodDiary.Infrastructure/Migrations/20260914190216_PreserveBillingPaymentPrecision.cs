using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class PreserveBillingPaymentPrecision : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AlterColumn<decimal>(
                name: "Tax",
                table: "BillingPayments",
                type: "numeric(19,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true,
                precision: 19,
                oldPrecision: 18,
                scale: 3,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "PayoutEarnings",
                table: "BillingPayments",
                type: "numeric(19,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true,
                precision: 19,
                oldPrecision: 18,
                scale: 3,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Fee",
                table: "BillingPayments",
                type: "numeric(19,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true,
                precision: 19,
                oldPrecision: 18,
                scale: 3,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Earnings",
                table: "BillingPayments",
                type: "numeric(19,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true,
                precision: 19,
                oldPrecision: 18,
                scale: 3,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "BillingPayments",
                type: "numeric(19,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true,
                precision: 19,
                oldPrecision: 18,
                scale: 3,
                oldScale: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            // Refuse a rollback that would silently round recorded provider money.
            migrationBuilder.Sql("""
                LOCK TABLE "BillingPayments" IN ACCESS EXCLUSIVE MODE;
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM "BillingPayments"
                        WHERE "Amount" <> round("Amount", 2)
                           OR "Tax" <> round("Tax", 2)
                           OR "Fee" <> round("Fee", 2)
                           OR "Earnings" <> round("Earnings", 2)
                           OR "PayoutEarnings" <> round("PayoutEarnings", 2)
                    ) THEN
                        RAISE EXCEPTION 'Cannot reduce billing precision while three-decimal payment amounts exist';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "Tax",
                table: "BillingPayments",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(19,3)",
                oldNullable: true,
                precision: 18,
                oldPrecision: 19,
                scale: 2,
                oldScale: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "PayoutEarnings",
                table: "BillingPayments",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(19,3)",
                oldNullable: true,
                precision: 18,
                oldPrecision: 19,
                scale: 2,
                oldScale: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "Fee",
                table: "BillingPayments",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(19,3)",
                oldNullable: true,
                precision: 18,
                oldPrecision: 19,
                scale: 2,
                oldScale: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "Earnings",
                table: "BillingPayments",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(19,3)",
                oldNullable: true,
                precision: 18,
                oldPrecision: 19,
                scale: 2,
                oldScale: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "BillingPayments",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(19,3)",
                oldNullable: true,
                precision: 18,
                oldPrecision: 19,
                scale: 2,
                oldScale: 3);
        }
    }
}
