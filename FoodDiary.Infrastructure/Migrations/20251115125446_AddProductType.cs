using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'Products'
                          AND column_name = 'ProductType'
                    ) THEN
                        ALTER TABLE "Products"
                            ADD COLUMN "ProductType" integer NOT NULL DEFAULT 0;
                    END IF;
                END ;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO 
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'Products'
                          AND column_name = 'ProductType'
                    ) THEN
                        ALTER TABLE "Products"
                            DROP COLUMN "ProductType";
                    END IF;
                END ;
                """);
        }
    }
}
