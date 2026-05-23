using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class AddUserRoleAuditEventUserForeignKeys : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddForeignKey(
                name: "FK_UserRoleAuditEvents_Users_ActorUserId",
                table: "UserRoleAuditEvents",
                column: "ActorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoleAuditEvents_Users_UserId",
                table: "UserRoleAuditEvents",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoleAuditEvents_Users_ActorUserId",
                table: "UserRoleAuditEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoleAuditEvents_Users_UserId",
                table: "UserRoleAuditEvents");
        }
    }
}
