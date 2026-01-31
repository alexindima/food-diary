using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

public partial class AddTelegramUserIdToUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "TelegramUserId",
            table: "Users",
            type: "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_TelegramUserId",
            table: "Users",
            column: "TelegramUserId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Users_TelegramUserId",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "TelegramUserId",
            table: "Users");
    }
}
