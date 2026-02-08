using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                  IF to_regclass('public."EmailTemplates"') IS NOT NULL THEN
                    INSERT INTO "EmailTemplates"
                        ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc")
                    SELECT
                        '9f5d2a1b-2d2d-4f12-9c4f-3a0bd0d4e6c1',
                        'email_verification',
                        'en',
                        'Confirm your email',
                        $$<!doctype html>
                <html lang="en">
                  <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>Confirm your email</title>
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
                                <h1 style="margin:0 0 12px;font-size:22px;line-height:1.3;">Confirm your email</h1>
                                <p style="margin:0 0 20px;font-size:15px;line-height:1.6;color:#475569;">Thanks for registering in {{brand}}.</p>
                                <table role="presentation" cellspacing="0" cellpadding="0">
                                  <tr>
                                    <td style="border-radius:10px;background:#4a90e2;">
                                      <a href="{{link}}" style="display:inline-block;padding:12px 20px;font-size:15px;color:#ffffff;text-decoration:none;font-weight:600;">
                                        Confirm email
                                      </a>
                                    </td>
                                  </tr>
                                </table>
                                <p style="margin:20px 0 0;font-size:13px;line-height:1.6;color:#64748b;">If you did not request this, you can ignore this email.</p>
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
                </html>$$,
                        $$Thanks for registering in {{brand}}.
                Please confirm your email: {{link}}
                If you did not request this, you can ignore this email.$$,
                        true,
                        TIMESTAMPTZ '2026-02-08T00:00:00Z',
                        NULL
                    WHERE NOT EXISTS (
                        SELECT 1 FROM "EmailTemplates" WHERE "Key" = 'email_verification' AND "Locale" = 'en'
                    );

                    INSERT INTO "EmailTemplates"
                        ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc")
                    SELECT
                        '3b8b6b24-7e11-4e0a-9f94-93b1a2a2a1b6',
                        'email_verification',
                        'ru',
                        '??????????? email',
                        $$<!doctype html>
                <html lang="ru">
                  <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>??????????? email</title>
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
                                <h1 style="margin:0 0 12px;font-size:22px;line-height:1.3;">??????????? email</h1>
                                <p style="margin:0 0 20px;font-size:15px;line-height:1.6;color:#475569;">??????? ?? ??????????? ? {{brand}}.</p>
                                <table role="presentation" cellspacing="0" cellpadding="0">
                                  <tr>
                                    <td style="border-radius:10px;background:#4a90e2;">
                                      <a href="{{link}}" style="display:inline-block;padding:12px 20px;font-size:15px;color:#ffffff;text-decoration:none;font-weight:600;">
                                        ??????????? email
                                      </a>
                                    </td>
                                  </tr>
                                </table>
                                <p style="margin:20px 0 0;font-size:13px;line-height:1.6;color:#64748b;">???? ?? ?? ??????????? ???, ?????? ?????????????? ??????.</p>
                              </td>
                            </tr>
                            <tr>
                              <td style="padding:16px 28px;background:#f8fafc;color:#94a3b8;font-family:Segoe UI,Arial,sans-serif;font-size:12px;">
                                ???? ?????? ?? ????????, ?????????? ?????? ? ???????:<br>
                                <span style="word-break:break-all;color:#64748b;">{{link}}</span>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                    </table>
                  </body>
                </html>$$,
                        $$??????? ?? ??????????? ? {{brand}}.
                ??????????? email: {{link}}
                ???? ?? ?? ??????????? ???, ?????? ?????????????? ??????.$$,
                        true,
                        TIMESTAMPTZ '2026-02-08T00:00:00Z',
                        NULL
                    WHERE NOT EXISTS (
                        SELECT 1 FROM "EmailTemplates" WHERE "Key" = 'email_verification' AND "Locale" = 'ru'
                    );

                    INSERT INTO "EmailTemplates"
                        ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc")
                    SELECT
                        'bdfd6e52-9b7b-4e8a-9f64-6c0c78a0d4fa',
                        'password_reset',
                        'en',
                        'Reset your password',
                        $$<!doctype html>
                <html lang="en">
                  <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>Reset your password</title>
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
                                <h1 style="margin:0 0 12px;font-size:22px;line-height:1.3;">Reset your password</h1>
                                <p style="margin:0 0 20px;font-size:15px;line-height:1.6;color:#475569;">We received a request to reset your {{brand}} password.</p>
                                <table role="presentation" cellspacing="0" cellpadding="0">
                                  <tr>
                                    <td style="border-radius:10px;background:#4a90e2;">
                                      <a href="{{link}}" style="display:inline-block;padding:12px 20px;font-size:15px;color:#ffffff;text-decoration:none;font-weight:600;">
                                        Reset password
                                      </a>
                                    </td>
                                  </tr>
                                </table>
                                <p style="margin:20px 0 0;font-size:13px;line-height:1.6;color:#64748b;">If you did not request this, you can ignore this email.</p>
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
                </html>$$,
                        $$We received a request to reset your {{brand}} password.
                Reset your password: {{link}}
                If you did not request this, you can ignore this email.$$,
                        true,
                        TIMESTAMPTZ '2026-02-08T00:00:00Z',
                        NULL
                    WHERE NOT EXISTS (
                        SELECT 1 FROM "EmailTemplates" WHERE "Key" = 'password_reset' AND "Locale" = 'en'
                    );

                    INSERT INTO "EmailTemplates"
                        ("Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc")
                    SELECT
                        'c3b2a7b1-6c91-4d6b-82b3-4f9aaf0c0b3f',
                        'password_reset',
                        'ru',
                        '????? ??????',
                        $$<!doctype html>
                <html lang="ru">
                  <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>????? ??????</title>
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
                                <h1 style="margin:0 0 12px;font-size:22px;line-height:1.3;">????? ??????</h1>
                                <p style="margin:0 0 20px;font-size:15px;line-height:1.6;color:#475569;">?? ???????? ?????? ?? ????? ?????? {{brand}}.</p>
                                <table role="presentation" cellspacing="0" cellpadding="0">
                                  <tr>
                                    <td style="border-radius:10px;background:#4a90e2;">
                                      <a href="{{link}}" style="display:inline-block;padding:12px 20px;font-size:15px;color:#ffffff;text-decoration:none;font-weight:600;">
                                        ???????? ??????
                                      </a>
                                    </td>
                                  </tr>
                                </table>
                                <p style="margin:20px 0 0;font-size:13px;line-height:1.6;color:#64748b;">???? ?? ?? ??????????? ???, ?????? ?????????????? ??????.</p>
                              </td>
                            </tr>
                            <tr>
                              <td style="padding:16px 28px;background:#f8fafc;color:#94a3b8;font-family:Segoe UI,Arial,sans-serif;font-size:12px;">
                                ???? ?????? ?? ????????, ?????????? ?????? ? ???????:<br>
                                <span style="word-break:break-all;color:#64748b;">{{link}}</span>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                    </table>
                  </body>
                </html>$$,
                        $$?? ???????? ?????? ?? ????? ?????? {{brand}}.
                ???????? ??????: {{link}}
                ???? ?? ?? ??????????? ???, ?????? ?????????????? ??????.$$,
                        true,
                        TIMESTAMPTZ '2026-02-08T00:00:00Z',
                        NULL
                    WHERE NOT EXISTS (
                        SELECT 1 FROM "EmailTemplates" WHERE "Key" = 'password_reset' AND "Locale" = 'ru'
                    );
                  END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "EmailTemplates"
                WHERE ("Key", "Locale") IN (
                    ('email_verification', 'en'),
                    ('email_verification', 'ru'),
                    ('password_reset', 'en'),
                    ('password_reset', 'ru')
                );
                """);
        }
    }
}
