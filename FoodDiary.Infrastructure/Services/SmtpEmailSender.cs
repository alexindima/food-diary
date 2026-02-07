using System;
using System.Net;
using System.Net.Mail;
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
        var htmlBody = $"""
            <p>Thanks for registering in FoodDiary.</p>
            <p>Please confirm your email:</p>
            <p><a href="{link}">Confirm email</a></p>
            <p>If you did not request this, you can ignore this email.</p>
            """;
        var textBody = $"Confirm your email: {link}";
        return SendAsync(message.ToEmail, subject, htmlBody, textBody, cancellationToken);
    }

    public Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken)
    {
        var link = BuildLink(_options.PasswordResetPath, message.UserId, message.Token);
        var subject = "Reset your password";
        var htmlBody = $"""
            <p>We received a request to reset your FoodDiary password.</p>
            <p>Reset your password:</p>
            <p><a href="{link}">Reset password</a></p>
            <p>If you did not request this, you can ignore this email.</p>
            """;
        var textBody = $"Reset your password: {link}";
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

    private async Task SendAsync(string toEmail, string subject, string htmlBody, string textBody, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpHost))
        {
            throw new InvalidOperationException("Email SMTP host is not configured.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject
        };

        message.To.Add(new MailAddress(toEmail));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html"));
        if (!string.IsNullOrWhiteSpace(textBody))
        {
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain"));
        }

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.UseSsl,
            Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPassword)
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}
