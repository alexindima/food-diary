using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyAdvices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var createdOn = new DateTime(2025, 12, 12, 0, 0, 0, DateTimeKind.Utc);

            migrationBuilder.CreateTable(
                name: "DailyAdvices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Tag = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyAdvices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyAdvices_Locale_Tag",
                table: "DailyAdvices",
                columns: new[] { "Locale", "Tag" });

            migrationBuilder.InsertData(
                table: "DailyAdvices",
                columns: new[]
                {
                    "Id", "Locale", "Value", "Weight", "Tag", "CreatedOnUtc", "ModifiedOnUtc"
                },
                values: new object[,]
                {
                    { new Guid("fce76c5a-3066-46aa-9193-3c8cbdb06f50"), "en", "Start your day with a glass of water before coffee.", 3, "hydration", createdOn, null },
                    { new Guid("bd3f9b4a-3a42-44fb-ac3b-6c5ccda5beaf"), "en", "Add a protein source to your breakfast to stay full longer.", 3, "protein", createdOn, null },
                    { new Guid("6a3ecbbf-4706-4e0c-bd8b-328f96ed58a1"), "en", "Keep chopped veggies or berries ready for quick snacks.", 2, "prep", createdOn, null },
                    { new Guid("9e3ee3be-9fcb-4de1-8ee4-17663fd42de2"), "en", "Plan a 10-minute walk after meals to stabilize energy.", 2, "movement", createdOn, null },
                    { new Guid("1bb2b64a-d13c-439f-9909-cc038bd74fa8"), "en", "Pre-log dinner to stay within your calorie target.", 1, "planning", createdOn, null },
                    { new Guid("78c1d056-e4bd-4236-9e76-6aeff024927c"), "ru", "Начните день со стакана воды до кофе.", 3, "hydration", createdOn, null },
                    { new Guid("d7e3b08a-93dd-4f35-8e0c-4df445f64ae8"), "ru", "Добавьте источник белка к завтраку, чтобы дольше держать сытость.", 3, "protein", createdOn, null },
                    { new Guid("c887d6e8-7d8f-4a91-9af8-9b777787a8f2"), "ru", "Держите нарезанные овощи или ягоды под рукой для быстрых перекусов.", 2, "prep", createdOn, null },
                    { new Guid("ba59c34b-282f-41bb-9e5e-91debf9cc5ee"), "ru", "Запланируйте 10-минутную прогулку после еды, чтобы стабилизировать энергию.", 2, "movement", createdOn, null },
                    { new Guid("7c26d24d-7df2-4553-8c09-c22b7a9c780b"), "ru", "Заранее занесите ужин в дневник, чтобы уложиться в калораж.", 1, "planning", createdOn, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyAdvices");
        }
    }
}
