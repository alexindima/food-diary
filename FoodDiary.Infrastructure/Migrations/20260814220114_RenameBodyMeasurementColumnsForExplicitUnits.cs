using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public partial class RenameBodyMeasurementColumnsForExplicitUnits : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.RenameColumn(
            name: "TargetWeight",
            table: "WeightGoals",
            newName: "TargetWeightKg");

        migrationBuilder.RenameColumn(
            name: "StartWeight",
            table: "WeightGoals",
            newName: "StartWeightKg");

        migrationBuilder.RenameColumn(
            name: "EndWeight",
            table: "WeightGoals",
            newName: "EndWeightKg");

        migrationBuilder.RenameColumn(
            name: "Weight",
            table: "WeightEntries",
            newName: "WeightKg");

        migrationBuilder.RenameColumn(
            name: "TargetWaist",
            table: "WaistGoals",
            newName: "TargetWaistCm");

        migrationBuilder.RenameColumn(
            name: "StartWaist",
            table: "WaistGoals",
            newName: "StartWaistCm");

        migrationBuilder.RenameColumn(
            name: "EndWaist",
            table: "WaistGoals",
            newName: "EndWaistCm");

        migrationBuilder.RenameColumn(
            name: "Circumference",
            table: "WaistEntries",
            newName: "CircumferenceCm");

        migrationBuilder.RenameColumn(
            name: "Weight",
            table: "Users",
            newName: "WeightKg");

        migrationBuilder.RenameColumn(
            name: "Height",
            table: "Users",
            newName: "HeightCm");

        migrationBuilder.RenameColumn(
            name: "DesiredWeight",
            table: "Users",
            newName: "DesiredWeightKg");

        migrationBuilder.RenameColumn(
            name: "DesiredWaist",
            table: "Users",
            newName: "DesiredWaistCm");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.RenameColumn(
            name: "TargetWeightKg",
            table: "WeightGoals",
            newName: "TargetWeight");

        migrationBuilder.RenameColumn(
            name: "StartWeightKg",
            table: "WeightGoals",
            newName: "StartWeight");

        migrationBuilder.RenameColumn(
            name: "EndWeightKg",
            table: "WeightGoals",
            newName: "EndWeight");

        migrationBuilder.RenameColumn(
            name: "WeightKg",
            table: "WeightEntries",
            newName: "Weight");

        migrationBuilder.RenameColumn(
            name: "TargetWaistCm",
            table: "WaistGoals",
            newName: "TargetWaist");

        migrationBuilder.RenameColumn(
            name: "StartWaistCm",
            table: "WaistGoals",
            newName: "StartWaist");

        migrationBuilder.RenameColumn(
            name: "EndWaistCm",
            table: "WaistGoals",
            newName: "EndWaist");

        migrationBuilder.RenameColumn(
            name: "CircumferenceCm",
            table: "WaistEntries",
            newName: "Circumference");

        migrationBuilder.RenameColumn(
            name: "WeightKg",
            table: "Users",
            newName: "Weight");

        migrationBuilder.RenameColumn(
            name: "HeightCm",
            table: "Users",
            newName: "Height");

        migrationBuilder.RenameColumn(
            name: "DesiredWeightKg",
            table: "Users",
            newName: "DesiredWeight");

        migrationBuilder.RenameColumn(
            name: "DesiredWaistCm",
            table: "Users",
            newName: "DesiredWaist");
    }
}
