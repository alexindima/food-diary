using System;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Models;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Services;

public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public Task SendEmailVerificationAsync(EmailVerificationMessage message, CancellationToken cancellationToken)
    {
        var link = BuildLink(_options.VerificationPath, message.UserId, message.Token);
        var subject = "Confirm your email";
        var htmlBody = BuildTemplate(
            title: "Confirm your email",
            intro: "Thanks for registering in FoodDiary.",
            ctaLabel: "Confirm email",
            ctaLink: link,
            footer: "If you did not request this, you can ignore this email.");
        var textBody = $"""
            Thanks for registering in FoodDiary.
            Please confirm your email: {link}
            If you did not request this, you can ignore this email.
            """;
        return SendAsync(message.ToEmail, subject, htmlBody, textBody, cancellationToken);
    }

    public Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken)
    {
        var link = BuildLink(_options.PasswordResetPath, message.UserId, message.Token);
        var subject = "Reset your password";
        var htmlBody = BuildTemplate(
            title: "Reset your password",
            intro: "We received a request to reset your FoodDiary password.",
            ctaLabel: "Reset password",
            ctaLink: link,
            footer: "If you did not request this, you can ignore this email.");
        var textBody = $"""
            We received a request to reset your FoodDiary password.
            Reset your password: {link}
            If you did not request this, you can ignore this email.
            """;
        return SendAsync(message.ToEmail, subject, htmlBody, textBody, cancellationToken);
    }

    private string BuildLink(string path, string userId, string token)
    {
        if (string.IsNullOrWhiteSpace(_options.FrontendBaseUrl))
        {
            throw new InvalidOperationException("Email FrontendBaseUrl is not configured.");
        }

        var baseUrl = _options.FrontendBaseUrl.TrimEnd('/');
        var safePath = path.StartsWith('/') ? path : "/" + path;
        return $"{baseUrl}{safePath}?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}";
    }

    private string BuildTemplate(string title, string intro, string ctaLabel, string ctaLink, string footer)
    {
        var brand = string.IsNullOrWhiteSpace(_options.FromName) ? "FoodDiary" : _options.FromName;
        return $"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>{title}</title>
              </head>
              <body style="margin:0;padding:0;background-color:#f4f6fb;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f4f6fb;padding:32px 16px;">
                  <tr>
                    <td align="center">
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#ffffff;border-radius:16px;box-shadow:0 12px 30px rgba(15,23,42,0.12);overflow:hidden;">
                        <tr>
                          <td style="padding:24px 28px;background:#101827;color:#ffffff;font-family:Segoe UI,Arial,sans-serif;font-size:18px;font-weight:600;">
                            {brand}
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:28px;font-family:Segoe UI,Arial,sans-serif;color:#0f172a;">
                            <h1 style="margin:0 0 12px;font-size:22px;line-height:1.3;">{title}</h1>
                            <p style="margin:0 0 20px;font-size:15px;line-height:1.6;color:#475569;">{intro}</p>
                            <table role="presentation" cellspacing="0" cellpadding="0">
                              <tr>
                                <td style="border-radius:10px;background:#4a90e2;">
                                  <a href="{ctaLink}" style="display:inline-block;padding:12px 20px;font-size:15px;color:#ffffff;text-decoration:none;font-weight:600;">
                                    {ctaLabel}
                                  </a>
                                </td>
                              </tr>
                            </table>
                            <p style="margin:20px 0 0;font-size:13px;line-height:1.6;color:#64748b;">{footer}</p>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:16px 28px;background:#f8fafc;color:#94a3b8;font-family:Segoe UI,Arial,sans-serif;font-size:12px;">
                            If the button doesn’t work, copy and paste this link into your browser:<br>
                            <span style="word-break:break-all;color:#64748b;">{ctaLink}</span>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
              </body>
            </html>
            """;
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody, string textBody, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpHost))
        {
            throw new InvalidOperationException("Email SMTP host is not configured.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };

        message.To.Add(new MailAddress(toEmail));
        if (!string.IsNullOrWhiteSpace(textBody))
        {
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, MediaTypeNames.Text.Plain));
        }
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, MediaTypeNames.Text.Html));

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.UseSsl,
            Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPassword)
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}
