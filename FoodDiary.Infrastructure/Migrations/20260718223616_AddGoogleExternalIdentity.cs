using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class AddGoogleExternalIdentity : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<string>(
                name: "GoogleIssuer",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleSubject",
                table: "Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "UserRefreshTokenSessions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_Users_GoogleIssuer_GoogleSubject",
                table: "Users",
                columns: ["GoogleIssuer", "GoogleSubject"],
                unique: true,
                filter: "\"GoogleIssuer\" IS NOT NULL AND \"GoogleSubject\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_Users_GoogleIssuer_GoogleSubject",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GoogleIssuer",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GoogleSubject",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "UserRefreshTokenSessions");
        }
    }
}
