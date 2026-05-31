using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    public partial class AddAdminUser : Migration {
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.Sql("""
                -- Initial admin bootstrap moved to host InitialAdmin options.
                SELECT 1;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.Sql(@"DELETE FROM ""Users"" WHERE ""Email"" = 'admin@fooddiary.club';");
        }
    }
}
