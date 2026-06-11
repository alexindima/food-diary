using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class ReplaceCyclesWithCycleProfiles : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "CycleDays");

            migrationBuilder.DropTable(
                name: "Cycles");

            migrationBuilder.CreateTable(
                name: "CycleProfiles",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Confidence = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TrackingStartDate = table.Column<DateTime>(type: "date", nullable: false),
                    AverageCycleLength = table.Column<int>(type: "integer", nullable: false, defaultValue: 28),
                    AveragePeriodLength = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    LutealLength = table.Column<int>(type: "integer", nullable: false, defaultValue: 14),
                    IsRegular = table.Column<bool>(type: "boolean", nullable: false),
                    IsOnboardingComplete = table.Column<bool>(type: "boolean", nullable: false),
                    ShowFertilityEstimates = table.Column<bool>(type: "boolean", nullable: false),
                    DiscreetNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_CycleProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CycleProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CycleBleedingEntries",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Flow = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PainImpact = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_CycleBleedingEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CycleBleedingEntries_CycleProfiles_CycleProfileId",
                        column: x => x.CycleProfileId,
                        principalTable: "CycleProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CycleFactors",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_CycleFactors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CycleFactors_CycleProfiles_CycleProfileId",
                        column: x => x.CycleProfileId,
                        principalTable: "CycleProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CycleSymptomEntries",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Intensity = table.Column<int>(type: "integer", nullable: false),
                    TagsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Note = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_CycleSymptomEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CycleSymptomEntries_CycleProfiles_CycleProfileId",
                        column: x => x.CycleProfileId,
                        principalTable: "CycleProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FertilitySignals",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    BasalBodyTemperatureCelsius = table.Column<double>(type: "double precision", nullable: true),
                    OvulationTestResult = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CervicalFluid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    HadSex = table.Column<bool>(type: "boolean", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_FertilitySignals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FertilitySignals_CycleProfiles_CycleProfileId",
                        column: x => x.CycleProfileId,
                        principalTable: "CycleProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CycleBleedingEntries_CycleProfileId_Date_Type",
                table: "CycleBleedingEntries",
                columns: ["CycleProfileId", "Date", "Type"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CycleFactors_CycleProfileId_Type_StartDate",
                table: "CycleFactors",
                columns: ["CycleProfileId", "Type", "StartDate"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CycleProfiles_UserId",
                table: "CycleProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CycleSymptomEntries_CycleProfileId_Date_Category",
                table: "CycleSymptomEntries",
                columns: ["CycleProfileId", "Date", "Category"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FertilitySignals_CycleProfileId_Date",
                table: "FertilitySignals",
                columns: ["CycleProfileId", "Date"],
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "CycleBleedingEntries");

            migrationBuilder.DropTable(
                name: "CycleFactors");

            migrationBuilder.DropTable(
                name: "CycleSymptomEntries");

            migrationBuilder.DropTable(
                name: "FertilitySignals");

            migrationBuilder.DropTable(
                name: "CycleProfiles");

            migrationBuilder.CreateTable(
                name: "Cycles",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AverageLength = table.Column<int>(type: "integer", nullable: false, defaultValue: 28),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LutealLength = table.Column<int>(type: "integer", nullable: false, defaultValue: 14),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_Cycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cycles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CycleDays",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    IsPeriod = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Edema = table.Column<int>(type: "integer", nullable: false),
                    Energy = table.Column<int>(type: "integer", nullable: false),
                    Headache = table.Column<int>(type: "integer", nullable: false),
                    Libido = table.Column<int>(type: "integer", nullable: false),
                    Mood = table.Column<int>(type: "integer", nullable: false),
                    Pain = table.Column<int>(type: "integer", nullable: false),
                    SleepQuality = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_CycleDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CycleDays_Cycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "Cycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CycleDays_CycleId_Date",
                table: "CycleDays",
                columns: ["CycleId", "Date"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cycles_User_StartDate",
                table: "Cycles",
                columns: ["UserId", "StartDate"]);
        }
    }
}
