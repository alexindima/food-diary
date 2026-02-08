using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Locale = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    TextBody = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Key_Locale",
                table: "EmailTemplates",
                columns: new[] { "Key", "Locale" },
                unique: true);

            migrationBuilder.InsertData(
                table: "EmailTemplates",
                columns: new[] { "Id", "Key", "Locale", "Subject", "HtmlBody", "TextBody", "IsActive", "CreatedOnUtc", "ModifiedOnUtc" },
                values: new object[,]
                {
                    {
                        new Guid("9f5d2a1b-2d2d-4f12-9c4f-3a0bd0d4e6c1"),
                        "email_verification",
                        "en",
                        "Confirm your email",
                        """
                        <!doctype html>
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
                        </html>
                        """,
                        """
                        Thanks for registering in {{brand}}.
                        Please confirm your email: {{link}}
                        If you did not request this, you can ignore this email.
                        """,
                        true,
                        new DateTime(2026, 2, 8, 0, 0, 0, DateTimeKind.Utc),
                        null
                    },
                    {
                        new Guid("3b8b6b24-7e11-4e0a-9f94-93b1a2a2a1b6"),
                        "email_verification",
                        "ru",
                        "Подтвердите email",
                        """
                        <!doctype html>
                        <html lang="ru">
                          <head>
                            <meta charset="UTF-8">
                            <meta name="viewport" content="width=device-width, initial-scale=1.0">
                            <title>Подтвердите email</title>
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
                                        <h1 style="margin:0 0 12px;font-size:22px;line-height:1.3;">Подтвердите email</h1>
                                        <p style="margin:0 0 20px;font-size:15px;line-height:1.6;color:#475569;">Спасибо за регистрацию в {{brand}}.</p>
                                        <table role="presentation" cellspacing="0" cellpadding="0">
                                          <tr>
                                            <td style="border-radius:10px;background:#4a90e2;">
                                              <a href="{{link}}" style="display:inline-block;padding:12px 20px;font-size:15px;color:#ffffff;text-decoration:none;font-weight:600;">
                                                Подтвердить email
                                              </a>
                                            </td>
                                          </tr>
                                        </table>
                                        <p style="margin:20px 0 0;font-size:13px;line-height:1.6;color:#64748b;">Если вы не запрашивали это, просто проигнорируйте письмо.</p>
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
                        </html>
                        """,
                        """
                        Спасибо за регистрацию в {{brand}}.
                        Подтвердите email: {{link}}
                        Если вы не запрашивали это, просто проигнорируйте письмо.
                        """,
                        true,
                        new DateTime(2026, 2, 8, 0, 0, 0, DateTimeKind.Utc),
                        null
                    },
                    {
                        new Guid("bdfd6e52-9b7b-4e8a-9f64-6c0c78a0d4fa"),
                        "password_reset",
                        "en",
                        "Reset your password",
                        """
                        <!doctype html>
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
                        </html>
                        """,
                        """
                        We received a request to reset your {{brand}} password.
                        Reset your password: {{link}}
                        If you did not request this, you can ignore this email.
                        """,
                        true,
                        new DateTime(2026, 2, 8, 0, 0, 0, DateTimeKind.Utc),
                        null
                    },
                    {
                        new Guid("c3b2a7b1-6c91-4d6b-82b3-4f9aaf0c0b3f"),
                        "password_reset",
                        "ru",
                        "Сброс пароля",
                        """
                        <!doctype html>
                        <html lang="ru">
                          <head>
                            <meta charset="UTF-8">
                            <meta name="viewport" content="width=device-width, initial-scale=1.0">
                            <title>Сброс пароля</title>
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
                                        <h1 style="margin:0 0 12px;font-size:22px;line-height:1.3;">Сброс пароля</h1>
                                        <p style="margin:0 0 20px;font-size:15px;line-height:1.6;color:#475569;">Мы получили запрос на смену пароля {{brand}}.</p>
                                        <table role="presentation" cellspacing="0" cellpadding="0">
                                          <tr>
                                            <td style="border-radius:10px;background:#4a90e2;">
                                              <a href="{{link}}" style="display:inline-block;padding:12px 20px;font-size:15px;color:#ffffff;text-decoration:none;font-weight:600;">
                                                Сбросить пароль
                                              </a>
                                            </td>
                                          </tr>
                                        </table>
                                        <p style="margin:20px 0 0;font-size:13px;line-height:1.6;color:#64748b;">Если вы не запрашивали это, просто проигнорируйте письмо.</p>
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
                        </html>
                        """,
                        """
                        Мы получили запрос на смену пароля {{brand}}.
                        Сбросить пароль: {{link}}
                        Если вы не запрашивали это, просто проигнорируйте письмо.
                        """,
                        true,
                        new DateTime(2026, 2, 8, 0, 0, 0, DateTimeKind.Utc),
                        null
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailTemplates");
        }
    }
}
