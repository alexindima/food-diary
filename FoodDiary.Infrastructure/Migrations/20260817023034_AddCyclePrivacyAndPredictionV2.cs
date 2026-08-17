using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddCyclePrivacyAndPredictionV2 : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<string>(
                name: "Goal",
                table: "CycleProfiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HideFromDashboard",
                table: "CycleProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReproductiveState",
                table: "CycleProfiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "CycleProfiles"
                SET "Goal" = CASE
                        WHEN "Mode" = 'TryingToConceive' THEN 'TryingToConceive'
                        ELSE 'PeriodAwareness'
                    END,
                    "ReproductiveState" = CASE
                        WHEN "Mode" = 'Pregnancy' THEN 'Pregnancy'
                        WHEN "Mode" = 'PostpartumLactation' THEN 'Postpartum'
                        WHEN "Mode" = 'Perimenopause' THEN 'Perimenopause'
                        WHEN "Mode" = 'NoPeriod' THEN 'NoPeriod'
                        ELSE 'Cycling'
                    END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Goal",
                table: "CycleProfiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReproductiveState",
                table: "CycleProfiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "CycleConsents",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GrantedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_CycleConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CycleConsents_CycleProfiles_CycleProfileId",
                        column: x => x.CycleProfileId,
                        principalTable: "CycleProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CyclePredictionRevisions",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextPeriodStartFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    NextPeriodStartTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Confidence = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DataSufficiency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PatternConsistency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CompletedCycleCount = table.Column<int>(type: "integer", nullable: false),
                    CalibrationSampleCount = table.Column<int>(type: "integer", nullable: false),
                    HistoricalCoveragePercent = table.Column<double>(type: "double precision", nullable: true),
                    MeanAbsoluteErrorDays = table.Column<double>(type: "double precision", nullable: true),
                    ReasonCodes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_CyclePredictionRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CyclePredictionRevisions_CycleProfiles_CycleProfileId",
                        column: x => x.CycleProfileId,
                        principalTable: "CycleProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CycleConsents_CycleProfileId_Purpose",
                table: "CycleConsents",
                columns: ["CycleProfileId", "Purpose"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CyclePredictionRevisions_CycleProfileId_GeneratedAtUtc",
                table: "CyclePredictionRevisions",
                columns: ["CycleProfileId", "GeneratedAtUtc"]);

            migrationBuilder.Sql(
                """
                INSERT INTO "CycleConsents" (
                    "Id", "CycleProfileId", "Purpose", "GrantedAtUtc", "RevokedAtUtc", "CreatedOnUtc", "ModifiedOnUtc")
                SELECT gen_random_uuid(), "Id", 'CycleTracking', "CreatedOnUtc", NULL, "CreatedOnUtc", NULL
                FROM "CycleProfiles";

                INSERT INTO "CycleConsents" (
                    "Id", "CycleProfileId", "Purpose", "GrantedAtUtc", "RevokedAtUtc", "CreatedOnUtc", "ModifiedOnUtc")
                SELECT gen_random_uuid(), profile."Id", 'FertilitySignals', profile."CreatedOnUtc", NULL, profile."CreatedOnUtc", NULL
                FROM "CycleProfiles" profile
                WHERE EXISTS (
                    SELECT 1
                    FROM "FertilitySignals" signal
                    WHERE signal."CycleProfileId" = profile."Id");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "CycleConsents");

            migrationBuilder.DropTable(
                name: "CyclePredictionRevisions");

            migrationBuilder.DropColumn(
                name: "Goal",
                table: "CycleProfiles");

            migrationBuilder.DropColumn(
                name: "HideFromDashboard",
                table: "CycleProfiles");

            migrationBuilder.DropColumn(
                name: "ReproductiveState",
                table: "CycleProfiles");
        }
    }
}
