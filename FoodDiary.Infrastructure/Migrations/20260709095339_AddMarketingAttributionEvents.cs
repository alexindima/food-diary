using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public partial class AddMarketingAttributionEvents : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            name: "MarketingAttributionEvents",
            columns: table => new {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: true),
                EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                AnonymousId = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                SessionId = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                LandingPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                ReferrerHost = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                UtmSource = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                UtmMedium = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                UtmCampaign = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                UtmContent = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                UtmTerm = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                BuildVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table => {
                table.PrimaryKey("PK_MarketingAttributionEvents", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MarketingAttributionEvents_AnonymousId_OccurredAtUtc",
            table: "MarketingAttributionEvents",
            columns: ["AnonymousId", "OccurredAtUtc"]);

        migrationBuilder.CreateIndex(
            name: "IX_MarketingAttributionEvents_OccurredAtUtc",
            table: "MarketingAttributionEvents",
            column: "OccurredAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_MarketingAttributionEvents_SessionId_OccurredAtUtc",
            table: "MarketingAttributionEvents",
            columns: ["SessionId", "OccurredAtUtc"]);

        migrationBuilder.CreateIndex(
            name: "IX_MarketingAttributionEvents_UtmSource_UtmMedium_UtmCampaign_~",
            table: "MarketingAttributionEvents",
            columns: ["UtmSource", "UtmMedium", "UtmCampaign", "OccurredAtUtc"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            name: "MarketingAttributionEvents");
    }
}
