using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class ConfirmImageUploads : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<bool>(
                name: "IsConfirmed",
                table: "ImageObjectDeletionOutbox",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsConfirmed",
                table: "ImageAssets",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("ALTER TABLE \"ImageObjectDeletionOutbox\" ALTER COLUMN \"IsConfirmed\" DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE \"ImageAssets\" ALTER COLUMN \"IsConfirmed\" DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropColumn(
                name: "IsConfirmed",
                table: "ImageObjectDeletionOutbox");

            migrationBuilder.DropColumn(
                name: "IsConfirmed",
                table: "ImageAssets");
        }
    }
}
