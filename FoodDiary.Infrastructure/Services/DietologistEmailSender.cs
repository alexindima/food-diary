using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Services;

public sealed class DietologistEmailSender(
    IOptions<EmailOptions> options,
    IEmailTransport emailTransport) : IDietologistEmailSender {
    private readonly EmailOptions _options = options.Value;

    public async Task SendDietologistInvitationAsync(
        DietologistInvitationMessage message,
        CancellationToken cancellationToken = default) {
        var link = BuildInvitationLink(message.InvitationId, message.Token);
        var isRu = NormalizeLanguage(message.Language) == "ru";
        var clientName = BuildClientName(message.ClientFirstName, message.ClientLastName);

        var subject = isRu
            ? "Приглашение стать диетологом"
            : "Invitation to become a dietologist";
        var intro = isRu
            ? $"{clientName} приглашает вас стать их диетологом в FoodDiary. Нажмите кнопку ниже, чтобы принять приглашение."
            : $"{clientName} has invited you to become their dietologist on FoodDiary. Click the button below to accept the invitation.";
        var ctaLabel = isRu ? "Принять приглашение" : "Accept Invitation";
        var footer = isRu
            ? "Если вы не ожидали это приглашение, просто проигнорируйте письмо."
            : "If you did not expect this invitation, you can ignore this email.";

        var brand = string.IsNullOrWhiteSpace(_options.FromName) ? "FoodDiary" : _options.FromName;
        var htmlBody = BuildTemplate(subject, intro, ctaLabel, link, footer, brand);
        var textBody = $"{intro}\n\n{ctaLabel}: {link}\n\n{footer}";

        await SendAsync(message.ToEmail, subject, htmlBody, textBody, cancellationToken);
    }

    private string BuildInvitationLink(Guid invitationId, string token) {
        if (string.IsNullOrWhiteSpace(_options.FrontendBaseUrl)) {
            throw new InvalidOperationException("Email FrontendBaseUrl is not configured.");
        }

        var baseUrl = _options.FrontendBaseUrl.TrimEnd('/');
        return $"{baseUrl}/dietologist/accept?invitationId={Uri.EscapeDataString(invitationId.ToString())}&token={Uri.EscapeDataString(token)}";
    }

    private static string BuildClientName(string? firstName, string? lastName) {
        var name = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? "A user" : name;
    }

    private static string NormalizeLanguage(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return "en";
        }

        var lower = value.Trim().ToLowerInvariant();
        return lower.StartsWith("ru") ? "ru" : "en";
    }

    private static string BuildTemplate(string title, string intro, string ctaLabel, string ctaLink, string footer, string brand) =>
        $"""
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

        await emailTransport.SendAsync(message, cancellationToken);
    }
}
