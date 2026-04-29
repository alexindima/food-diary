using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class AddOwnerRole : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            var ownerRoleId = Guid.Parse("f8c47d75-cb3f-4267-9ec5-761dc6934211");

            migrationBuilder.Sql($@"
INSERT INTO ""Roles"" (""Id"", ""Name"", ""CreatedOnUtc"")
VALUES ('{ownerRoleId}', 'Owner', NOW())
ON CONFLICT (""Name"") DO NOTHING;

INSERT INTO ""UserRoles"" (""UserId"", ""RoleId"")
SELECT u.""Id"", r.""Id""
FROM ""Users"" u
CROSS JOIN ""Roles"" r
WHERE u.""Email"" = 'admin@fooddiary.club'
  AND r.""Name"" IN ('Owner', 'Admin')
ON CONFLICT DO NOTHING;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.Sql(@"
DELETE FROM ""UserRoles"" ur
USING ""Users"" u, ""Roles"" r
WHERE ur.""UserId"" = u.""Id""
  AND ur.""RoleId"" = r.""Id""
  AND u.""Email"" = 'admin@fooddiary.club'
  AND r.""Name"" = 'Owner';

DELETE FROM ""Roles""
WHERE ""Name"" = 'Owner';
");
        }
    }
}
