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
        IEmailTemplateProvider templateProvider = CreateTemplateProvider(out Func<string?> getLastKey, out Func<string?> getLastLocale);
        IEmailTransport transport = CreateCapturingTransport(out Func<SentEmail> getSent);
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

        Assert.Equal("email_verification", getLastKey());
        Assert.Equal("en", getLastLocale());
        Assert.Equal("user@example.com", getSent().ToEmail);
        Assert.Equal("Confirm your email", getSent().Subject);
        Assert.Contains("https://tenant.example/verify-email?userId=user%201&token=token%2Fvalue", getSent().Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendPasswordReset_WithTemplate_AppliesTokens() {
        Dictionary<(string Key, string Locale), EmailTemplateContent> templates = new() {
            [("password_reset", "ru")] = new EmailTemplateContent(
                "Reset {{brand}}",
                "<p>{{brand}} {{link}}</p>",
                "Plain {{brand}} {{link}}"),
        };
        IEmailTemplateProvider templateProvider = CreateTemplateProvider(templates);
        IEmailTransport transport = CreateCapturingTransport(out Func<SentEmail> getSent);
        var sender = new EmailSender(CreateOptions(fromName: "FD"), templateProvider, transport);

        await sender.SendPasswordResetAsync(
            new PasswordResetMessage("user@example.com", "user-1", "token", "ru"),
            CancellationToken.None);

        Assert.Equal("Reset FD", getSent().Subject);
        Assert.Contains("<p>FD https://app.example/reset-password?userId=user-1&token=token</p>", getSent().Body, StringComparison.Ordinal);
        Assert.Contains(getSent().AlternateViewBodies, body => body.Contains("Plain FD https://app.example/reset-password", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SendTestEmail_WithRussianLanguage_SendsRussianSubjectAndPlainText() {
        IEmailTransport transport = CreateCapturingTransport(out Func<SentEmail> getSent);
        var sender = new EmailSender(CreateOptions(), CreateTemplateProvider(), transport);

        await sender.SendTestEmailAsync(new TestEmailMessage("user@example.com", "ru-RU"), CancellationToken.None);

        Assert.Equal("user@example.com", getSent().ToEmail);
        Assert.Contains("FoodDiary", getSent().Subject, StringComparison.Ordinal);
        Assert.Contains("MailRelay", getSent().Body, StringComparison.Ordinal);
        Assert.Contains(getSent().AlternateViewBodies, body => body.Contains("MailRelay", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SendTestEmail_WithBlankLanguage_UsesEnglishFallback() {
        IEmailTransport transport = CreateCapturingTransport(out Func<SentEmail> getSent);
        var sender = new EmailSender(CreateOptions(), CreateTemplateProvider(), transport);

        await sender.SendTestEmailAsync(new TestEmailMessage("user@example.com", " "), CancellationToken.None);

        Assert.Equal("FoodDiary test email", getSent().Subject);
        Assert.Contains("main email dispatch path is working", getSent().Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendTestEmail_WhenTransportFails_Rethrows() {
        var sender = new EmailSender(CreateOptions(), CreateTemplateProvider(), CreateThrowingTransport());

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendTestEmailAsync(new TestEmailMessage("user@example.com", "en"), CancellationToken.None));

        Assert.Equal("transport failed", ex.Message);
    }

    [Fact]
    public async Task SendEmailVerification_WhenFrontendBaseUrlMissing_Throws() {
        var sender = new EmailSender(
            CreateOptions(frontendBaseUrl: ""),
            CreateTemplateProvider(),
            CreateCapturingTransport(out _));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendEmailVerificationAsync(
                new EmailVerificationMessage("user@example.com", "user", "token", "en"),
                CancellationToken.None));
    }

    [Fact]
    public async Task SendDietologistInvitation_WithFallbackContent_SendsInvitationLink() {
        IEmailTransport transport = CreateCapturingTransport(out Func<SentEmail> getSent);
        var sender = new DietologistEmailSender(CreateOptions(), CreateTemplateProvider(), transport);
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

        Assert.Equal("dietologist@example.com", getSent().ToEmail);
        Assert.Equal("Invitation to become a dietologist", getSent().Subject);
        Assert.Contains("Alex Ivanov", getSent().Body, StringComparison.Ordinal);
        Assert.Contains($"invitationId={invitationId}", getSent().Body, StringComparison.Ordinal);
        Assert.Contains("token=invite%20token", getSent().Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendDietologistInvitation_WithTemplate_AppliesClientNameToken() {
        Dictionary<(string Key, string Locale), EmailTemplateContent> templates = new() {
            [("dietologist_invitation", "ru")] = new EmailTemplateContent(
                "Invite {{clientName}}",
                "<p>{{clientName}} {{brand}} {{link}}</p>",
                "{{clientName}} {{brand}} {{link}}"),
        };
        IEmailTemplateProvider templateProvider = CreateTemplateProvider(templates);
        IEmailTransport transport = CreateCapturingTransport(out Func<SentEmail> getSent);
        var sender = new DietologistEmailSender(CreateOptions(fromName: "FD"), templateProvider, transport);

        await sender.SendDietologistInvitationAsync(
            new DietologistInvitationMessage(
                "dietologist@example.com",
                Guid.NewGuid(),
                "token",
                ClientFirstName: null,
                ClientLastName: null,
                "ru"),
            CancellationToken.None);

        Assert.Equal("Invite A user", getSent().Subject);
        Assert.Contains("<p>A user FD https://app.example/dietologist/accept?", getSent().Body, StringComparison.Ordinal);
        Assert.Contains(getSent().AlternateViewBodies, body => body.Contains("A user FD https://app.example/dietologist/accept?", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SendDietologistInvitation_WhenFrontendBaseUrlMissing_Throws() {
        var sender = new DietologistEmailSender(
            CreateOptions(frontendBaseUrl: ""),
            CreateTemplateProvider(),
            CreateCapturingTransport(out _));

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
            AllowedFrontendBaseUrls = allowedFrontendBaseUrls ?? [],
        };

    private static IEmailTemplateProvider CreateTemplateProvider() =>
        CreateTemplateProvider(new Dictionary<(string Key, string Locale), EmailTemplateContent>(), out _, out _);

    private static IEmailTemplateProvider CreateTemplateProvider(
        IReadOnlyDictionary<(string Key, string Locale), EmailTemplateContent> templates) =>
        CreateTemplateProvider(templates, out _, out _);

    private static IEmailTemplateProvider CreateTemplateProvider(
        out Func<string?> getLastKey,
        out Func<string?> getLastLocale) =>
        CreateTemplateProvider(new Dictionary<(string Key, string Locale), EmailTemplateContent>(), out getLastKey, out getLastLocale);

    private static IEmailTemplateProvider CreateTemplateProvider(
        IReadOnlyDictionary<(string Key, string Locale), EmailTemplateContent> templates,
        out Func<string?> getLastKey,
        out Func<string?> getLastLocale) {
        IEmailTemplateProvider templateProvider = Substitute.For<IEmailTemplateProvider>();
        string? lastKey = null;
        string? lastLocale = null;
        templateProvider
            .GetActiveTemplateAsync(
                Arg.Do<string>(key => lastKey = key),
                Arg.Do<string>(locale => lastLocale = locale),
                Arg.Any<CancellationToken>())
            .Returns(call => {
                string key = call.ArgAt<string>(0);
                string locale = call.ArgAt<string>(1);
                return Task.FromResult(templates.GetValueOrDefault((key, locale)));
            });
        getLastKey = () => lastKey;
        getLastLocale = () => lastLocale;
        return templateProvider;
    }

    private static IEmailTransport CreateCapturingTransport(out Func<SentEmail> getSent) {
        IEmailTransport transport = Substitute.For<IEmailTransport>();
        SentEmail sent = new(
            ToEmail: null,
            Subject: null,
            Body: null,
            AlternateViewBodies: []);
        transport
            .SendAsync(Arg.Do<MailMessage>(message => {
                List<string> alternateViewBodies = [];
                foreach (AlternateView view in message.AlternateViews) {
                    using var reader = new StreamReader(view.ContentStream);
                    alternateViewBodies.Add(reader.ReadToEnd());
                }

                sent = new SentEmail(
                    ToEmail: message.To.Single().Address,
                    Subject: message.Subject,
                    Body: message.Body,
                    AlternateViewBodies: alternateViewBodies);
            }), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        getSent = () => sent;
        return transport;
    }

    private static IEmailTransport CreateThrowingTransport() {
        IEmailTransport transport = Substitute.For<IEmailTransport>();
        transport
            .SendAsync(Arg.Any<MailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("transport failed")));
        return transport;
    }

    [ExcludeFromCodeCoverage]
    private sealed record SentEmail(
        string? ToEmail,
        string? Subject,
        string? Body,
        IReadOnlyList<string> AlternateViewBodies);
}
