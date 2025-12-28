using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

public partial class SetDefaultLanguageForUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE \"Users\" SET \"Language\" = 'en' WHERE \"Language\" IS NULL;");

        migrationBuilder.AlterColumn<string>(
            name: "Language",
            table: "Users",
            type: "text",
            nullable: true,
            defaultValue: "en",
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Language",
            table: "Users",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true,
            oldDefaultValue: "en");
    }
}
