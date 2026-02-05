using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

public partial class AddUserRoles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Roles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Roles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "UserRoles",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                RoleId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey(
                    name: "FK_UserRoles_Roles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserRoles_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Roles_Name",
            table: "Roles",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserRoles_RoleId",
            table: "UserRoles",
            column: "RoleId");

        var adminRoleId = Guid.Parse("7bb7a1f7-9dfc-4e2a-9497-3b5b29d299a1");
        var premiumRoleId = Guid.Parse("e25d9e58-1c78-42f1-9f7c-9c3f6c4e6b2c");
        var supportRoleId = Guid.Parse("b2fb6dfc-2e86-4a4b-9a20-2e8a5b8a2d9c");
        var adminUserId = Guid.Parse("9a5f9b2b-f50b-4e2d-8a4a-8d1a58c7a901");

        migrationBuilder.Sql($@"
INSERT INTO ""Roles"" (""Id"", ""Name"", ""CreatedOnUtc"")
VALUES
    ('{adminRoleId}', 'Admin', NOW()),
    ('{premiumRoleId}', 'Premium', NOW()),
    ('{supportRoleId}', 'Support', NOW())
ON CONFLICT (""Name"") DO NOTHING;
");

        migrationBuilder.Sql($@"
INSERT INTO ""UserRoles"" (""UserId"", ""RoleId"")
SELECT '{adminUserId}', r.""Id""
FROM ""Roles"" r
WHERE r.""Name"" IN ('Admin', 'Premium')
  AND EXISTS (SELECT 1 FROM ""Users"" u WHERE u.""Id"" = '{adminUserId}')
ON CONFLICT DO NOTHING;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "UserRoles");

        migrationBuilder.DropTable(
            name: "Roles");
    }
}
