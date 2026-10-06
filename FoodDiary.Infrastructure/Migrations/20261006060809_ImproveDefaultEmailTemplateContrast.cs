using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public partial class ImproveDefaultEmailTemplateContrast : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) =>
        UpdateDefaults(migrationBuilder, upgrading: true);

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) =>
        UpdateDefaults(migrationBuilder, upgrading: false);

    private static void UpdateDefaults(MigrationBuilder migrationBuilder, bool upgrading) {
        string fromButton = upgrading ? "#4a90e2" : "#2563eb";
        string toButton = upgrading ? "#2563eb" : "#4a90e2";
        string fromFooter = upgrading ? "#94a3b8" : "#64748b";
        string toFooter = upgrading ? "#64748b" : "#94a3b8";
        string expectedHashColumn = upgrading ? "OldHash" : "NewHash";
        // Full seeded HTML protects custom content while preserving all other template fields.
        migrationBuilder.Sql(
            $"""
            UPDATE "EmailTemplates" AS template
            SET "HtmlBody" = replace(replace(template."HtmlBody", 'background:{fromButton};', 'background:{toButton};'),
                'background:#f8fafc;color:{fromFooter};', 'background:#f8fafc;color:{toFooter};')
            FROM (VALUES
                ('9f5d2a1b-2d2d-4f12-9c4f-3a0bd0d4e6c1'::uuid, 'email_verification', 'en', '17e363222cb132143820b551d136d83d', '6e5df480f1eb32e4fad63af0f8fbc08f'),
                ('3b8b6b24-7e11-4e0a-9f94-93b1a2a2a1b6'::uuid, 'email_verification', 'ru', '3c81b85207488995688699b59eb9148d', '03fced8547fbe7c9859e87b8cd599e6d'),
                ('bdfd6e52-9b7b-4e8a-9f64-6c0c78a0d4fa'::uuid, 'password_reset', 'en', '26f32272e13663642f045fe77f89290b', '11c527d0d4abeb097ec1f937a62b4760'),
                ('c3b2a7b1-6c91-4d6b-82b3-4f9aaf0c0b3f'::uuid, 'password_reset', 'ru', 'a3f9fa99ecdb82af339d6427e90cddae', '8245860d18cd3120a2fb8c0f7d629b33'),
                ('70d06c63-b046-4bc0-89be-5636d5d030ef'::uuid, 'dietologist_invitation', 'en', '36f3923f01c49dd1d750ffd930c107cb', '94ba5b42f12db35f6aa4ee7679409d75'),
                ('2ebcb2a1-368f-4191-970d-18353f959696'::uuid, 'dietologist_invitation', 'ru', '853fbd2260b09fbbac4a8fd64e6910fb', '3bc88e2b66cd6fa8addd987edd943b92')
            ) AS defaults("Id", "Key", "Locale", "OldHash", "NewHash")
            WHERE template."Id" = defaults."Id"
              AND template."Key" = defaults."Key"
              AND template."Locale" = defaults."Locale"
              AND md5(replace(template."HtmlBody", E'\r\n', E'\n')) = defaults."{expectedHashColumn}";
            """);
    }
}
