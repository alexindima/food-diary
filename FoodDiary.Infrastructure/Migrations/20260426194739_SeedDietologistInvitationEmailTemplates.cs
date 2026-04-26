using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
public partial class SeedDietologistInvitationEmailTemplates : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql(
            """
            INSERT INTO "EmailTemplates"
                ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc")
            SELECT
                '70d06c63-b046-4bc0-89be-5636d5d030ef',
                'dietologist_invitation',
                'en',
                'Invitation to become a dietologist',
                $html$<!doctype html>
            <html lang="en">
              <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Invitation to become a dietologist</title>
              </head>
              <body style="margin:0;padding:0;background-color:#f4f6fb;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f4f6fb;padding:32px 16px;">
                  <tr>
                    <td align="center">
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#ffffff;border-radius:16px;box-shadow:0 12px 30px rgba(15,23,42,0.12);overflow:hidden;">
                        <tr>
                          <td style="padding:24px 28px;background:#101827;color:#ffffff;font-family:Segoe UI,Arial,sans-serif;font-size:18px;font-weight:600;">
                            {{brand}}
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:28px;font-family:Segoe UI,Arial,sans-serif;color:#0f172a;">
                            <h1 style="margin:0 0 12px;font-size:22px;line-height:1.3;">Invitation to become a dietologist</h1>
                            <p style="margin:0 0 20px;font-size:15px;line-height:1.6;color:#475569;">{{clientName}} has invited you to become their dietologist on {{brand}}. Click the button below to accept the invitation.</p>
                            <table role="presentation" cellspacing="0" cellpadding="0">
                              <tr>
                                <td style="border-radius:10px;background:#4a90e2;">
                                  <a href="{{link}}" style="display:inline-block;padding:12px 20px;font-size:15px;color:#ffffff;text-decoration:none;font-weight:600;">
                                    Accept Invitation
                                  </a>
                                </td>
                              </tr>
                            </table>
                            <p style="margin:20px 0 0;font-size:13px;line-height:1.6;color:#64748b;">If you did not expect this invitation, you can ignore this email.</p>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:16px 28px;background:#f8fafc;color:#94a3b8;font-family:Segoe UI,Arial,sans-serif;font-size:12px;">
                            If the button doesn't work, copy and paste this link into your browser:<br>
                            <span style="word-break:break-all;color:#64748b;">{{link}}</span>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
              </body>
            </html>$html$,
                $text${{clientName}} has invited you to become their dietologist on {{brand}}.
            Accept Invitation: {{link}}
            If you did not expect this invitation, you can ignore this email.$text$,
                true,
                TIMESTAMPTZ '2026-04-26T00:00:00Z',
                NULL
            WHERE NOT EXISTS (
                SELECT 1 FROM "EmailTemplates" WHERE "Key" = 'dietologist_invitation' AND "Locale" = 'en'
            );

            INSERT INTO "EmailTemplates"
                ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc")
            SELECT
                '2ebcb2a1-368f-4191-970d-18353f959696',
                'dietologist_invitation',
                'ru',
                'Приглашение стать диетологом',
                $html$<!doctype html>
            <html lang="ru">
              <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Приглашение стать диетологом</title>
              </head>
              <body style="margin:0;padding:0;background-color:#f4f6fb;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f4f6fb;padding:32px 16px;">
                  <tr>
                    <td align="center">
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#ffffff;border-radius:16px;box-shadow:0 12px 30px rgba(15,23,42,0.12);overflow:hidden;">
                        <tr>
                          <td style="padding:24px 28px;background:#101827;color:#ffffff;font-family:Segoe UI,Arial,sans-serif;font-size:18px;font-weight:600;">
                            {{brand}}
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:28px;font-family:Segoe UI,Arial,sans-serif;color:#0f172a;">
                            <h1 style="margin:0 0 12px;font-size:22px;line-height:1.3;">Приглашение стать диетологом</h1>
                            <p style="margin:0 0 20px;font-size:15px;line-height:1.6;color:#475569;">{{clientName}} приглашает вас стать их диетологом в {{brand}}. Нажмите кнопку ниже, чтобы принять приглашение.</p>
                            <table role="presentation" cellspacing="0" cellpadding="0">
                              <tr>
                                <td style="border-radius:10px;background:#4a90e2;">
                                  <a href="{{link}}" style="display:inline-block;padding:12px 20px;font-size:15px;color:#ffffff;text-decoration:none;font-weight:600;">
                                    Принять приглашение
                                  </a>
                                </td>
                              </tr>
                            </table>
                            <p style="margin:20px 0 0;font-size:13px;line-height:1.6;color:#64748b;">Если вы не ожидали это приглашение, просто проигнорируйте письмо.</p>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:16px 28px;background:#f8fafc;color:#94a3b8;font-family:Segoe UI,Arial,sans-serif;font-size:12px;">
                            Если кнопка не работает, скопируйте ссылку в браузер:<br>
                            <span style="word-break:break-all;color:#64748b;">{{link}}</span>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
              </body>
            </html>$html$,
                $text${{clientName}} приглашает вас стать их диетологом в {{brand}}.
            Принять приглашение: {{link}}
            Если вы не ожидали это приглашение, просто проигнорируйте письмо.$text$,
                true,
                TIMESTAMPTZ '2026-04-26T00:00:00Z',
                NULL
            WHERE NOT EXISTS (
                SELECT 1 FROM "EmailTemplates" WHERE "Key" = 'dietologist_invitation' AND "Locale" = 'ru'
            );
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql(
            """
            DELETE FROM "EmailTemplates"
            WHERE ("Key", "Locale") IN (
                ('dietologist_invitation', 'en'),
                ('dietologist_invitation', 'ru')
            );
            """);
    }
}
