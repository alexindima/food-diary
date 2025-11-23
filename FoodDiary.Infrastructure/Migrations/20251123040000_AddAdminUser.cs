using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations
{
    public partial class AddAdminUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string email = "admin@fooddiary.club";
            var adminId = Guid.Parse("9a5f9b2b-f50b-4e2d-8a4a-8d1a58c7a901");
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("123456");
            var createdOn = DateTime.UtcNow.ToString("O");

            migrationBuilder.Sql($@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM ""Users"" WHERE ""Email"" = '{email}') THEN
        INSERT INTO ""Users"" (""Id"", ""Email"", ""Password"", ""IsActive"", ""CreatedOnUtc"", ""ActivityLevel"")
        VALUES ('{adminId}', '{email}', '{passwordHash}', TRUE, '{createdOn}', 'Moderate');
    END IF;
END
$$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""Users"" WHERE ""Email"" = 'admin@fooddiary.club';");
        }
    }
}
