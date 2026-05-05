using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class AddUserLoginEvents : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "UserLoginEvents",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthProvider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    BrowserName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BrowserVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    OperatingSystem = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DeviceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    LoggedInAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_UserLoginEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLoginEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginEvents_AuthProvider",
                table: "UserLoginEvents",
                column: "AuthProvider");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginEvents_BrowserName",
                table: "UserLoginEvents",
                column: "BrowserName");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginEvents_DeviceType",
                table: "UserLoginEvents",
                column: "DeviceType");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginEvents_LoggedInAtUtc",
                table: "UserLoginEvents",
                column: "LoggedInAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginEvents_OperatingSystem",
                table: "UserLoginEvents",
                column: "OperatingSystem");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginEvents_UserId",
                table: "UserLoginEvents",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "UserLoginEvents");
        }
    }
}
