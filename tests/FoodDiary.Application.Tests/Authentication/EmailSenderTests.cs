using System.Net.Mail;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Dietologist.Services;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class EmailSenderTests {
    [Fact]
    public async Task SendEmailVerification_WithAllowedClientOrigin_UsesFallbackAndTenantOrigin() {
        var templateProvider = new StubEmailTemplateProvider();
        var transport = new RecordingEmailTransport();
        var sender = new EmailSender(
            CreateOptions(allowedFrontendBaseUrls: ["https://tenant.example/"]),
            templateProvider,
            transport);

        await sender.SendEmailVerificationAsync(
            new EmailVerificationMessage(
                "user@example.com",
                "user 1",
                "token/value",
                "en-US",
                "https://TENANT.example/shell"),
            CancellationToken.None);

        Assert.Equal("email_verification", templateProvider.LastKey);
        Assert.Equal("en", templateProvider.LastLocale);
        Assert.Equal("user@example.com", transport.ToEmail);
        Assert.Equal("Confirm your email", transport.Subject);
        Assert.Contains("https://tenant.example/verify-email?userId=user%201&token=token%2Fvalue", transport.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendPasswordReset_WithTemplate_AppliesTokens() {
        var templateProvider = new StubEmailTemplateProvider();
        templateProvider.Seed(
            "password_reset",
            "ru",
            new EmailTemplateContent(
                "Reset {{brand}}",
                "<p>{{brand}} {{link}}</p>",
                "Plain {{brand}} {{link}}"));
        var transport = new RecordingEmailTransport();
        var sender = new EmailSender(CreateOptions(fromName: "FD"), templateProvider, transport);

        await sender.SendPasswordResetAsync(
            new PasswordResetMessage("user@example.com", "user-1", "token", "ru"),
            CancellationToken.None);

        Assert.Equal("Reset FD", transport.Subject);
        Assert.Contains("<p>FD https://app.example/reset-password?userId=user-1&token=token</p>", transport.Body, StringComparison.Ordinal);
        Assert.Contains(transport.AlternateViewBodies, body => body.Contains("Plain FD https://app.example/reset-password", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SendTestEmail_WithRussianLanguage_SendsRussianSubjectAndPlainText() {
        var transport = new RecordingEmailTransport();
        var sender = new EmailSender(CreateOptions(), new StubEmailTemplateProvider(), transport);

        await sender.SendTestEmailAsync(new TestEmailMessage("user@example.com", "ru-RU"), CancellationToken.None);

        Assert.Equal("user@example.com", transport.ToEmail);
        Assert.Contains("FoodDiary", transport.Subject, StringComparison.Ordinal);
        Assert.Contains("MailRelay", transport.Body, StringComparison.Ordinal);
        Assert.Contains(transport.AlternateViewBodies, body => body.Contains("MailRelay", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SendTestEmail_WithBlankLanguage_UsesEnglishFallback() {
        var transport = new RecordingEmailTransport();
        var sender = new EmailSender(CreateOptions(), new StubEmailTemplateProvider(), transport);

        await sender.SendTestEmailAsync(new TestEmailMessage("user@example.com", " "), CancellationToken.None);

        Assert.Equal("FoodDiary test email", transport.Subject);
        Assert.Contains("main email dispatch path is working", transport.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendTestEmail_WhenTransportFails_Rethrows() {
        var sender = new EmailSender(CreateOptions(), new StubEmailTemplateProvider(), new ThrowingEmailTransport());

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendTestEmailAsync(new TestEmailMessage("user@example.com", "en"), CancellationToken.None));

        Assert.Equal("transport failed", ex.Message);
    }

    [Fact]
    public async Task SendEmailVerification_WhenFrontendBaseUrlMissing_Throws() {
        var sender = new EmailSender(
            CreateOptions(frontendBaseUrl: ""),
            new StubEmailTemplateProvider(),
            new RecordingEmailTransport());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendEmailVerificationAsync(
                new EmailVerificationMessage("user@example.com", "user", "token", "en"),
                CancellationToken.None));
    }

    [Fact]
    public async Task SendDietologistInvitation_WithFallbackContent_SendsInvitationLink() {
        var transport = new RecordingEmailTransport();
        var sender = new DietologistEmailSender(CreateOptions(), new StubEmailTemplateProvider(), transport);
        var invitationId = Guid.NewGuid();

        await sender.SendDietologistInvitationAsync(
            new DietologistInvitationMessage(
                "dietologist@example.com",
                invitationId,
                "invite token",
                "Alex",
                "Ivanov",
                "en"),
            CancellationToken.None);

        Assert.Equal("dietologist@example.com", transport.ToEmail);
        Assert.Equal("Invitation to become a dietologist", transport.Subject);
        Assert.Contains("Alex Ivanov", transport.Body, StringComparison.Ordinal);
        Assert.Contains($"invitationId={invitationId}", transport.Body, StringComparison.Ordinal);
        Assert.Contains("token=invite%20token", transport.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendDietologistInvitation_WithTemplate_AppliesClientNameToken() {
        var templateProvider = new StubEmailTemplateProvider();
        templateProvider.Seed(
            "dietologist_invitation",
            "ru",
            new EmailTemplateContent(
                "Invite {{clientName}}",
                "<p>{{clientName}} {{brand}} {{link}}</p>",
                "{{clientName}} {{brand}} {{link}}"));
        var transport = new RecordingEmailTransport();
        var sender = new DietologistEmailSender(CreateOptions(fromName: "FD"), templateProvider, transport);

        await sender.SendDietologistInvitationAsync(
            new DietologistInvitationMessage(
                "dietologist@example.com",
                Guid.NewGuid(),
                "token",
                null,
                null,
                "ru"),
            CancellationToken.None);

        Assert.Equal("Invite A user", transport.Subject);
        Assert.Contains("<p>A user FD https://app.example/dietologist/accept?", transport.Body, StringComparison.Ordinal);
        Assert.Contains(transport.AlternateViewBodies, body => body.Contains("A user FD https://app.example/dietologist/accept?", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SendDietologistInvitation_WhenFrontendBaseUrlMissing_Throws() {
        var sender = new DietologistEmailSender(
            CreateOptions(frontendBaseUrl: ""),
            new StubEmailTemplateProvider(),
            new RecordingEmailTransport());

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendDietologistInvitationAsync(
                new DietologistInvitationMessage(
                    "dietologist@example.com",
                    Guid.NewGuid(),
                    "token",
                    "Alex",
                    "Ivanov",
                    "en"),
                CancellationToken.None));

        Assert.Equal("Email FrontendBaseUrl is not configured.", ex.Message);
    }

    private static EmailOptions CreateOptions(
        string frontendBaseUrl = "https://app.example",
        string fromName = "FoodDiary",
        string[]? allowedFrontendBaseUrls = null) =>
        new() {
            FromAddress = "noreply@example.com",
            FromName = fromName,
            FrontendBaseUrl = frontendBaseUrl,
            AllowedFrontendBaseUrls = allowedFrontendBaseUrls ?? []
        };

    [ExcludeFromCodeCoverage]
    private sealed class StubEmailTemplateProvider : IEmailTemplateProvider {
        private readonly Dictionary<(string Key, string Locale), EmailTemplateContent> _templates = [];

        public string? LastKey { get; private set; }
        public string? LastLocale { get; private set; }

        public void Seed(string key, string locale, EmailTemplateContent template) {
            _templates[(key, locale)] = template;
        }

        public Task<EmailTemplateContent?> GetActiveTemplateAsync(
            string key,
            string locale,
            CancellationToken cancellationToken = default) {
            LastKey = key;
            LastLocale = locale;
            return Task.FromResult(_templates.GetValueOrDefault((key, locale)));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingEmailTransport : IEmailTransport {
        public string? ToEmail { get; private set; }
        public string? Subject { get; private set; }
        public string? Body { get; private set; }
        public List<string> AlternateViewBodies { get; } = [];

        public async Task SendAsync(MailMessage message, CancellationToken cancellationToken) {
            ToEmail = message.To.Single().Address;
            Subject = message.Subject;
            Body = message.Body;

            foreach (AlternateView view in message.AlternateViews) {
                using var reader = new StreamReader(view.ContentStream);
                AlternateViewBodies.Add(await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false));
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingEmailTransport : IEmailTransport {
        public Task SendAsync(MailMessage message, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("transport failed");
    }
}
