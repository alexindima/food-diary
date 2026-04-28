using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class AddAdminImpersonationSessions : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "AdminImpersonationSessions",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ActorIpAddress = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ActorUserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_AdminImpersonationSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminImpersonationSessions_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdminImpersonationSessions_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminImpersonationSessions_ActorUserId",
                table: "AdminImpersonationSessions",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminImpersonationSessions_StartedAtUtc",
                table: "AdminImpersonationSessions",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AdminImpersonationSessions_TargetUserId",
                table: "AdminImpersonationSessions",
                column: "TargetUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "AdminImpersonationSessions");
        }
    }
}
