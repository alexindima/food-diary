using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Email.Common;
using FoodDiary.Application.Abstractions.Email.Common;
using System.Globalization;

namespace FoodDiary.Application.Authentication.Services;

public sealed class EmailSender(
    EmailOptions options,
    IEmailTemplateProvider templateProvider,
    IEmailTransport emailTransport,
    IEmailOutbox emailOutbox) : IEmailSender {
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

    public Task SendEmailVerificationAsync(EmailVerificationMessage message, CancellationToken cancellationToken) {
        string locale = NormalizeLanguage(message.Language);
        return SendEmailCoreAsync(
            templateKey: "email_verification",
            locale: locale,
            toEmail: message.ToEmail,
            buildLink: () => BuildLink(options.VerificationPath, message.UserId, message.Token, message.ClientOrigin),
            createFallbackContent: link => (
                Subject: string.Equals(locale, "ru", StringComparison.Ordinal) ? EmailVerificationSubjectRu : "Confirm your email",
                Html: string.Equals(locale, "ru"
                    , StringComparison.Ordinal)
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
                Text: string.Equals(locale, "ru"
                    , StringComparison.Ordinal)
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
        string locale = NormalizeLanguage(message.Language);
        return SendEmailCoreAsync(
            templateKey: "password_reset",
            locale: locale,
            toEmail: message.ToEmail,
            buildLink: () => BuildFragmentLink(options.PasswordResetPath, message.UserId, message.Token, message.ClientOrigin),
            createFallbackContent: link => (
                Subject: string.Equals(locale, "ru", StringComparison.Ordinal) ? PasswordResetSubjectRu : "Reset your password",
                Html: string.Equals(locale, "ru"
                    , StringComparison.Ordinal)
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
                Text: string.Equals(locale, "ru"
                    , StringComparison.Ordinal)
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

    public async Task SendTestEmailAsync(TestEmailMessage message, CancellationToken cancellationToken) {
        string locale = NormalizeLanguage(message.Language);
        string subject = string.Equals(locale, "ru"
            , StringComparison.Ordinal)
            ? "\u0422\u0435\u0441\u0442\u043e\u0432\u043e\u0435 \u043f\u0438\u0441\u044c\u043c\u043e FoodDiary"
            : "FoodDiary test email";
        string intro = string.Equals(locale, "ru"
            , StringComparison.Ordinal)
            ? "\u042d\u0442\u043e \u0442\u0435\u0441\u0442\u043e\u0432\u043e\u0435 \u043f\u0438\u0441\u044c\u043c\u043e \u043e\u0442\u043f\u0440\u0430\u0432\u043b\u0435\u043d\u043e \u0438\u0437 \u0432\u0430\u0448\u0435\u0433\u043e \u043b\u043e\u043a\u0430\u043b\u044c\u043d\u043e\u0433\u043e FoodDiary \u0447\u0435\u0440\u0435\u0437 MailRelay."
            : "This test email was sent from your local FoodDiary through MailRelay.";
        string footer = string.Equals(locale, "ru"
            , StringComparison.Ordinal)
            ? "\u0415\u0441\u043b\u0438 \u043f\u0438\u0441\u044c\u043c\u043e \u0434\u043e\u0448\u043b\u043e, \u043e\u0441\u043d\u043e\u0432\u043d\u043e\u0439 \u043f\u0443\u0442\u044c \u043e\u0442\u043f\u0440\u0430\u0432\u043a\u0438 \u0440\u0430\u0431\u043e\u0442\u0430\u0435\u0442."
            : "If this message arrived, the main email dispatch path is working.";

        try {
            await SendAsync(
                message.ToEmail,
                subject,
                BuildTemplate(subject, intro, "FoodDiary", options.FrontendBaseUrl, footer),
                intro + Environment.NewLine + footer,
                cancellationToken).ConfigureAwait(false);
            ApplicationEmailTelemetry.RecordEmailDispatch("dashboard_test_email", locale, "success");
        } catch (Exception ex) {
            ApplicationEmailTelemetry.RecordEmailDispatch("dashboard_test_email", locale, "failure", ex.GetType().Name);
            throw;
        }
    }

    private string BuildLink(string path, string userId, string token, string? clientOrigin) {
        string resolvedBaseUrl = ResolveFrontendBaseUrl(clientOrigin);
        if (string.IsNullOrWhiteSpace(resolvedBaseUrl)) {
            throw new InvalidOperationException("Email FrontendBaseUrl is not configured.");
        }

        string baseUrl = resolvedBaseUrl.TrimEnd('/');
        string safePath = path.StartsWith('/') ? path : "/" + path;
        return $"{baseUrl}{safePath}?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}";
    }

    private string BuildFragmentLink(string path, string userId, string token, string? clientOrigin) {
        string queryLink = BuildLink(path, userId, token, clientOrigin);
        int querySeparatorIndex = queryLink.IndexOf('?', StringComparison.Ordinal);
        return querySeparatorIndex < 0
            ? queryLink
            : queryLink[..querySeparatorIndex] + '#' + queryLink[(querySeparatorIndex + 1)..];
    }

    private string ResolveFrontendBaseUrl(string? clientOrigin) {
        string? requestedOrigin = NormalizeOrigin(clientOrigin);
        if (requestedOrigin is null) {
            return options.FrontendBaseUrl;
        }

        foreach (string allowedBaseUrl in GetAllowedFrontendBaseUrls()) {
            if (string.Equals(NormalizeOrigin(allowedBaseUrl), requestedOrigin, StringComparison.Ordinal)) {
                return allowedBaseUrl.TrimEnd('/');
            }
        }

        return options.FrontendBaseUrl;
    }

    private IEnumerable<string> GetAllowedFrontendBaseUrls() {
        if (!string.IsNullOrWhiteSpace(options.FrontendBaseUrl)) {
            yield return options.FrontendBaseUrl;
        }

        foreach (string value in options.AllowedFrontendBaseUrls) {
            if (!string.IsNullOrWhiteSpace(value)) {
                yield return value;
            }
        }
    }

    private static string? NormalizeOrigin(string? value) {
        if (string.IsNullOrWhiteSpace(value) ||
            !Uri.TryCreate(value.Trim(), UriKind.Absolute, out Uri? uri) ||
            (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal) && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal))) {
            return null;
        }

        string port = uri.IsDefaultPort ? string.Empty : string.Create(CultureInfo.InvariantCulture, $":{uri.Port}");
        return $"{uri.Scheme}://{uri.IdnHost.ToLowerInvariant()}{port}";
    }

    private static string NormalizeLanguage(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return "en";
        }

        string lower = value.Trim().ToLowerInvariant();
        return lower.StartsWith("ru", StringComparison.Ordinal) ? "ru" : "en";
    }

    private async Task SendEmailCoreAsync(
        string templateKey,
        string locale,
        string toEmail,
        Func<string> buildLink,
        Func<string, (string Subject, string Html, string Text)> createFallbackContent,
        CancellationToken cancellationToken) {
        try {
            string link = buildLink();
            EmailTemplateContent? template = await templateProvider.GetActiveTemplateAsync(templateKey, locale, cancellationToken).ConfigureAwait(false);
            string brand = string.IsNullOrWhiteSpace(options.FromName) ? "FoodDiary" : options.FromName;
            (string Subject, string Html, string Text) = createFallbackContent(link);

            string subject = template is null
                ? Subject
                : ApplyTemplateTokens(template.Subject, link, brand);
            string htmlBody = template is null || string.IsNullOrWhiteSpace(template.HtmlBody)
                ? Html
                : ApplyTemplateTokens(template.HtmlBody, link, brand);
            string textBody = template is null || string.IsNullOrWhiteSpace(template.TextBody)
                ? Text
                : ApplyTemplateTokens(template.TextBody, link, brand);

            await DispatchAsync(toEmail, subject, htmlBody, textBody, cancellationToken).ConfigureAwait(false);
            ApplicationEmailTelemetry.RecordEmailDispatch(templateKey, locale, "success");
        } catch (Exception ex) {
            ApplicationEmailTelemetry.RecordEmailDispatch(templateKey, locale, "failure", ex.GetType().Name);
            throw;
        }
    }

    private static string ApplyTemplateTokens(string value, string link, string brand) {
        return value
            .Replace("{{link}}", link, StringComparison.OrdinalIgnoreCase)
            .Replace("{{brand}}", brand, StringComparison.OrdinalIgnoreCase);
    }

    private string BuildTemplate(string title, string intro, string ctaLabel, string ctaLink, string footer) {
        string brand = string.IsNullOrWhiteSpace(options.FromName) ? "FoodDiary" : options.FromName;
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
        var message = new EmailMessage(
            options.FromAddress,
            options.FromName,
            [toEmail],
            subject,
            htmlBody,
            string.IsNullOrWhiteSpace(textBody) ? null : textBody);

        await emailTransport.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    private async Task DispatchAsync(string toEmail, string subject, string htmlBody, string textBody, CancellationToken cancellationToken) {
        var message = new EmailMessage(
            options.FromAddress,
            options.FromName,
            [toEmail],
            subject,
            htmlBody,
            string.IsNullOrWhiteSpace(textBody) ? null : textBody);

        await emailOutbox.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
