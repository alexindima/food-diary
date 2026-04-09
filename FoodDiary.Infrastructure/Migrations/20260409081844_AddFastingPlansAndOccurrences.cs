using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class AddFastingPlansAndOccurrences : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "FastingPlans",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Protocol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StoppedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IntermittentFastHours = table.Column<int>(type: "integer", nullable: true),
                    IntermittentEatingWindowHours = table.Column<int>(type: "integer", nullable: true),
                    ExtendedTargetHours = table.Column<int>(type: "integer", nullable: true),
                    CyclicFastDays = table.Column<int>(type: "integer", nullable: true),
                    CyclicEatDays = table.Column<int>(type: "integer", nullable: true),
                    CyclicEatDayFastHours = table.Column<int>(type: "integer", nullable: true),
                    CyclicEatDayEatingWindowHours = table.Column<int>(type: "integer", nullable: true),
                    CyclicAnchorDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CyclicNextPhaseDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_FastingPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FastingPlans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FastingOccurrences",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    ScheduledForUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InitialTargetHours = table.Column<int>(type: "integer", nullable: true),
                    AddedTargetHours = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_FastingOccurrences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FastingOccurrences_FastingPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "FastingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FastingOccurrences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FastingOccurrences_PlanId",
                table: "FastingOccurrences",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_FastingOccurrences_PlanId_SequenceNumber",
                table: "FastingOccurrences",
                columns: new[] { "PlanId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FastingOccurrences_UserId",
                table: "FastingOccurrences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FastingOccurrences_UserId_Status",
                table: "FastingOccurrences",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FastingPlans_UserId",
                table: "FastingPlans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FastingPlans_UserId_Status",
                table: "FastingPlans",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "FastingOccurrences");

            migrationBuilder.DropTable(
                name: "FastingPlans");
        }
    }
}
