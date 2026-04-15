using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddDietologistProfileAndFastingPermissions : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<bool>(
            name: "ShareFasting",
            table: "DietologistInvitations",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "ShareProfile",
            table: "DietologistInvitations",
            type: "boolean",
            nullable: false,
            defaultValue: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            name: "ShareFasting",
            table: "DietologistInvitations");

        migrationBuilder.DropColumn(
            name: "ShareProfile",
            table: "DietologistInvitations");
    }
}
