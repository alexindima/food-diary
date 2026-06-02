using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddUserRoleAuditEvents : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "UserRoleAuditEvents",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_UserRoleAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAuditEvents_Action",
                table: "UserRoleAuditEvents",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAuditEvents_ActorUserId",
                table: "UserRoleAuditEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAuditEvents_OccurredAtUtc",
                table: "UserRoleAuditEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAuditEvents_RoleId",
                table: "UserRoleAuditEvents",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAuditEvents_RoleName",
                table: "UserRoleAuditEvents",
                column: "RoleName");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAuditEvents_UserId",
                table: "UserRoleAuditEvents",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "UserRoleAuditEvents");
        }
    }
}
