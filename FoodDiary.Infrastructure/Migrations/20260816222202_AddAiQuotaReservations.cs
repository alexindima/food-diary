using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddAiQuotaReservations : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "AiQuotaPeriods",
                columns: table => new {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedInputTokens = table.Column<long>(type: "bigint", nullable: false),
                    ConsumedOutputTokens = table.Column<long>(type: "bigint", nullable: false),
                    ReservedInputTokens = table.Column<long>(type: "bigint", nullable: false),
                    ReservedOutputTokens = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_AiQuotaPeriods", x => new { x.UserId, x.PeriodStartUtc });
                    table.CheckConstraint("CK_AiQuotaPeriods_ConsumedInputTokens", "\"ConsumedInputTokens\" >= 0");
                    table.CheckConstraint("CK_AiQuotaPeriods_ConsumedOutputTokens", "\"ConsumedOutputTokens\" >= 0");
                    table.CheckConstraint("CK_AiQuotaPeriods_ReservedInputTokens", "\"ReservedInputTokens\" >= 0");
                    table.CheckConstraint("CK_AiQuotaPeriods_ReservedOutputTokens", "\"ReservedOutputTokens\" >= 0");
                    table.ForeignKey(
                        name: "FK_AiQuotaPeriods_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiQuotaReservations",
                columns: table => new {
                    RequestId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Operation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReservedInputTokens = table.Column<long>(type: "bigint", nullable: false),
                    ReservedOutputTokens = table.Column<long>(type: "bigint", nullable: false),
                    ActualInputTokens = table.Column<long>(type: "bigint", nullable: true),
                    ActualOutputTokens = table.Column<long>(type: "bigint", nullable: true),
                    State = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ExpiresOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_AiQuotaReservations", x => x.RequestId);
                    table.CheckConstraint("CK_AiQuotaReservations_ReservedInputTokens", "\"ReservedInputTokens\" >= 0");
                    table.CheckConstraint("CK_AiQuotaReservations_ReservedOutputTokens", "\"ReservedOutputTokens\" >= 0");
                    table.ForeignKey(
                        name: "FK_AiQuotaReservations_AiQuotaPeriods_UserId_PeriodStartUtc",
                        columns: x => new { x.UserId, x.PeriodStartUtc },
                        principalTable: "AiQuotaPeriods",
                        principalColumns: ["UserId", "PeriodStartUtc"],
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiQuotaReservations_UserId_PeriodStartUtc_State_ExpiresOnUtc",
                table: "AiQuotaReservations",
                columns: ["UserId", "PeriodStartUtc", "State", "ExpiresOnUtc"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "AiQuotaReservations");

            migrationBuilder.DropTable(
                name: "AiQuotaPeriods");
        }
    }
}
