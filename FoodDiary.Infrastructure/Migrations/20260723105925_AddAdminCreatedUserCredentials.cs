using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public partial class AddAdminCreatedUserCredentials : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<bool>(
            name: "MustChangePassword",
            table: "Users",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql(
            """
            INSERT INTO "EmailTemplates"
                ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc")
            SELECT
                gen_random_uuid(),
                seed."Key",
                seed."Locale",
                seed."Subject",
                seed."HtmlBody",
                seed."TextBody",
                TRUE,
                NOW(),
                NOW()
            FROM (
                VALUES
                    (
                        'account_created',
                        'en',
                        'Your {{brand}} account is ready',
                        '<h1>Your account is ready</h1><p>An account has been created for you.</p><p><strong>Email:</strong> {{email}}<br><strong>Temporary password:</strong> <code>{{temporaryPassword}}</code></p><p><a href="{{loginLink}}">Sign in</a></p><p>You will be asked to choose a new password after signing in.</p>',
                        E'Your account is ready.\n\nEmail: {{email}}\nTemporary password: {{temporaryPassword}}\nSign in: {{loginLink}}\n\nYou will be asked to choose a new password after signing in.'
                    ),
                    (
                        'account_created',
                        'ru',
                        'Ваш аккаунт {{brand}} готов',
                        '<h1>Ваш аккаунт готов</h1><p>Для вас создан аккаунт.</p><p><strong>Email:</strong> {{email}}<br><strong>Временный пароль:</strong> <code>{{temporaryPassword}}</code></p><p><a href="{{loginLink}}">Войти</a></p><p>После входа потребуется установить новый пароль.</p>',
                        E'Ваш аккаунт готов.\n\nEmail: {{email}}\nВременный пароль: {{temporaryPassword}}\nВойти: {{loginLink}}\n\nПосле входа потребуется установить новый пароль.'
                    )
            ) AS seed("Key", "Locale", "Subject", "HtmlBody", "TextBody")
            WHERE NOT EXISTS (
                SELECT 1
                FROM "EmailTemplates" existing
                WHERE existing."Key" = seed."Key" AND existing."Locale" = seed."Locale"
            );
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql(
            """
            DELETE FROM "EmailTemplates"
            WHERE "Key" = 'account_created' AND "Locale" IN ('en', 'ru');
            """);

        migrationBuilder.DropColumn(
            name: "MustChangePassword",
            table: "Users");
    }
}
