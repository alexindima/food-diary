using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Services;

public sealed class EmailSender(
    IOptions<EmailOptions> options,
    IEmailTemplateProvider templateProvider,
    IEmailTransport emailTransport) : IEmailSender {
    private const string EmailVerificationSubjectRu =
        "\u041f\u043e\u0434\u0442\u0432\u0435\u0440\u0434\u0438\u0442\u0435 email";
    private const string EmailVerificationIntroRu =
        "\u0421\u043f\u0430\u0441\u0438\u0431\u043e \u0437\u0430 \u0440\u0435\u0433\u0438\u0441\u0442\u0440\u0430\u0446\u0438\u044e \u0432 FoodDiary.";
    private const string EmailVerificationCtaRu =
        "\u041f\u043e\u0434\u0442\u0432\u0435\u0440\u0434\u0438\u0442\u044c email";
    private const string PasswordResetSubjectRu =
        "\u0421\u0431\u0440\u043e\u0441 \u043f\u0430\u0440\u043e\u043b\u044f";
    private const string PasswordResetIntroRu =
        "\u041c\u044b \u043f\u043e\u043b\u0443\u0447\u0438\u043b\u0438 \u0437\u0430\u043f\u0440\u043e\u0441 \u043d\u0430 \u0441\u043c\u0435\u043d\u0443 \u043f\u0430\u0440\u043e\u043b\u044f FoodDiary.";
    private const string PasswordResetCtaRu =
        "\u0421\u0431\u0440\u043e\u0441\u0438\u0442\u044c \u043f\u0430\u0440\u043e\u043b\u044c";
    private const string IgnoreEmailFooterRu =
        "\u0415\u0441\u043b\u0438 \u0432\u044b \u043d\u0435 \u0437\u0430\u043f\u0440\u0430\u0448\u0438\u0432\u0430\u043b\u0438 \u044d\u0442\u043e, \u043f\u0440\u043e\u0441\u0442\u043e \u043f\u0440\u043e\u0438\u0433\u043d\u043e\u0440\u0438\u0440\u0443\u0439\u0442\u0435 \u043f\u0438\u0441\u044c\u043c\u043e.";

    private readonly EmailOptions _options = options.Value;
    private readonly IEmailTemplateProvider _templateProvider = templateProvider;
    private readonly IEmailTransport _emailTransport = emailTransport;

    public Task SendEmailVerificationAsync(EmailVerificationMessage message, CancellationToken cancellationToken) {
        var locale = NormalizeLanguage(message.Language);
        return SendEmailCoreAsync(
            templateKey: "email_verification",
            locale: locale,
            toEmail: message.ToEmail,
            buildLink: () => BuildLink(_options.VerificationPath, message.UserId, message.Token),
            createFallbackContent: link => (
                Subject: locale == "ru" ? EmailVerificationSubjectRu : "Confirm your email",
                Html: locale == "ru"
                    ? BuildTemplate(
                        title: EmailVerificationSubjectRu,
                        intro: EmailVerificationIntroRu,
                        ctaLabel: EmailVerificationCtaRu,
                        ctaLink: link,
                        footer: IgnoreEmailFooterRu)
                    : BuildTemplate(
                        title: "Confirm your email",
                        intro: "Thanks for registering in FoodDiary.",
                        ctaLabel: "Confirm email",
                        ctaLink: link,
                        footer: "If you did not request this, you can ignore this email."),
                Text: locale == "ru"
                    ? $$"""
                      {{EmailVerificationIntroRu}}
                      {{EmailVerificationSubjectRu}}: {{link}}
                      {{IgnoreEmailFooterRu}}
                      """
                    : $"""
                       Thanks for registering in FoodDiary.
                       Please confirm your email: {link}
                       If you did not request this, you can ignore this email.
                       """),
            cancellationToken: cancellationToken);
    }

    public Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken) {
        var locale = NormalizeLanguage(message.Language);
        return SendEmailCoreAsync(
            templateKey: "password_reset",
            locale: locale,
            toEmail: message.ToEmail,
            buildLink: () => BuildLink(_options.PasswordResetPath, message.UserId, message.Token),
            createFallbackContent: link => (
                Subject: locale == "ru" ? PasswordResetSubjectRu : "Reset your password",
                Html: locale == "ru"
                    ? BuildTemplate(
                        title: PasswordResetSubjectRu,
                        intro: PasswordResetIntroRu,
                        ctaLabel: PasswordResetCtaRu,
                        ctaLink: link,
                        footer: IgnoreEmailFooterRu)
                    : BuildTemplate(
                        title: "Reset your password",
                        intro: "We received a request to reset your FoodDiary password.",
                        ctaLabel: "Reset password",
                        ctaLink: link,
                        footer: "If you did not request this, you can ignore this email."),
                Text: locale == "ru"
                    ? $$"""
                      {{PasswordResetIntroRu}}
                      {{PasswordResetCtaRu}}: {{link}}
                      {{IgnoreEmailFooterRu}}
                      """
                    : $"""
                       We received a request to reset your FoodDiary password.
                       Reset your password: {link}
                       If you did not request this, you can ignore this email.
                       """),
            cancellationToken: cancellationToken);
    }

    private string BuildLink(string path, string userId, string token) {
        if (string.IsNullOrWhiteSpace(_options.FrontendBaseUrl)) {
            throw new InvalidOperationException("Email FrontendBaseUrl is not configured.");
        }

        var baseUrl = _options.FrontendBaseUrl.TrimEnd('/');
        var safePath = path.StartsWith('/') ? path : "/" + path;
        return $"{baseUrl}{safePath}?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}";
    }

    private static string NormalizeLanguage(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return "en";
        }

        var lower = value.Trim().ToLowerInvariant();
        return lower.StartsWith("ru") ? "ru" : "en";
    }

    private async Task SendEmailCoreAsync(
        string templateKey,
        string locale,
        string toEmail,
        Func<string> buildLink,
        Func<string, (string Subject, string Html, string Text)> createFallbackContent,
        CancellationToken cancellationToken) {
        try {
            var link = buildLink();
            var template = await _templateProvider.GetActiveTemplateAsync(templateKey, locale, cancellationToken);
            var brand = string.IsNullOrWhiteSpace(_options.FromName) ? "FoodDiary" : _options.FromName;
            var fallback = createFallbackContent(link);

            var subject = template is null
                ? fallback.Subject
                : ApplyTemplateTokens(template.Subject, link, brand);
            var htmlBody = template is null || string.IsNullOrWhiteSpace(template.HtmlBody)
                ? fallback.Html
                : ApplyTemplateTokens(template.HtmlBody, link, brand);
            var textBody = template is null || string.IsNullOrWhiteSpace(template.TextBody)
                ? fallback.Text
                : ApplyTemplateTokens(template.TextBody, link, brand);

            await SendAsync(toEmail, subject, htmlBody, textBody, cancellationToken);
            InfrastructureTelemetry.RecordEmailDispatch(templateKey, locale, "success");
        } catch (Exception ex) {
            InfrastructureTelemetry.RecordEmailDispatch(templateKey, locale, "failure", ex.GetType().Name);
            throw;
        }
    }

    private static string ApplyTemplateTokens(string value, string link, string brand) {
        return value
            .Replace("{{link}}", link, StringComparison.OrdinalIgnoreCase)
            .Replace("{{brand}}", brand, StringComparison.OrdinalIgnoreCase);
    }

    private string BuildTemplate(string title, string intro, string ctaLabel, string ctaLink, string footer) {
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
                                If the button doesn't work, copy and paste this link into your browser:<br>
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

    private async Task SendAsync(string toEmail, string subject, string htmlBody, string textBody, CancellationToken cancellationToken) {
        using var message = new MailMessage {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };

        message.To.Add(new MailAddress(toEmail));
        if (!string.IsNullOrWhiteSpace(textBody)) {
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, MediaTypeNames.Text.Plain));
        }

        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, MediaTypeNames.Text.Html));

        await _emailTransport.SendAsync(message, cancellationToken);
    }
}
