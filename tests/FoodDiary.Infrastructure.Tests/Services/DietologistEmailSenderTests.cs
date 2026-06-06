using System.Net.Mail;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Dietologist.Services;
using FoodDiary.Application.Abstractions.Email.Common;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class DietologistEmailSenderTests {
    private static readonly EmailOptions DefaultOptions = new() {
        FromAddress = "noreply@fooddiary.club",
        FromName = "FoodDiary",
        FrontendBaseUrl = "https://fooddiary.club",
    };

    [Fact]
    public async Task SendDietologistInvitationAsync_SendsEmailToRecipient() {
        var transport = new RecordingEmailTransport();
        DietologistEmailSender sender = CreateSender(transport);
        DietologistInvitationMessage message = CreateMessage("diet@example.com");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Equal(1, transport.SentCount);
        Assert.Equal("diet@example.com", transport.LastRecipient);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_WithEnglishLocale_HasEnglishSubject() {
        var transport = new RecordingEmailTransport();
        DietologistEmailSender sender = CreateSender(transport);
        DietologistInvitationMessage message = CreateMessage("diet@example.com", language: "en");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Contains("Invitation", transport.LastSubject, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_WithRussianLocale_HasRussianSubject() {
        var transport = new RecordingEmailTransport();
        DietologistEmailSender sender = CreateSender(transport);
        DietologistInvitationMessage message = CreateMessage("diet@example.com", language: "ru");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Contains("\u041f\u0440\u0438\u0433\u043b\u0430\u0448\u0435\u043d\u0438\u0435", transport.LastSubject, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_IncludesInvitationLink() {
        var transport = new RecordingEmailTransport();
        DietologistEmailSender sender = CreateSender(transport);
        var invitationId = Guid.NewGuid();
        var message = new DietologistInvitationMessage(
            "diet@example.com", invitationId, "test-token", "John", "Doe", "en");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Contains("dietologist/accept", transport.LastHtmlBody, StringComparison.Ordinal);
        Assert.Contains(invitationId.ToString(), transport.LastHtmlBody, StringComparison.Ordinal);
        Assert.Contains("test-token", transport.LastHtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_IncludesClientName() {
        var transport = new RecordingEmailTransport();
        DietologistEmailSender sender = CreateSender(transport);
        var message = new DietologistInvitationMessage(
            "diet@example.com", Guid.NewGuid(), "token", "\u0410\u043b\u0435\u043a\u0441\u0435\u0439", "\u0418\u0432\u0430\u043d\u043e\u0432", "ru");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Contains("\u0410\u043b\u0435\u043a\u0441\u0435\u0439 \u0418\u0432\u0430\u043d\u043e\u0432", transport.LastHtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_WithNullClientName_UsesDefaultName() {
        var transport = new RecordingEmailTransport();
        DietologistEmailSender sender = CreateSender(transport);
        var message = new DietologistInvitationMessage(
            "diet@example.com", Guid.NewGuid(), "token", ClientFirstName: null, ClientLastName: null, "en");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Contains("A user", transport.LastHtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_WithNullLanguage_DefaultsToEnglish() {
        var transport = new RecordingEmailTransport();
        DietologistEmailSender sender = CreateSender(transport);
        var message = new DietologistInvitationMessage(
            "diet@example.com", Guid.NewGuid(), "token", "John", ClientLastName: null, Language: null);

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Contains("Invitation", transport.LastSubject, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_WithStoredTemplate_UsesTemplateTokens() {
        var transport = new RecordingEmailTransport();
        var templateProvider = new StubEmailTemplateProvider(new EmailTemplateContent(
            "Invite {{clientName}} to {{brand}}",
            "<p>{{clientName}}</p><a href=\"{{link}}\">{{brand}}</a>",
            "{{clientName}} {{link}} {{brand}}"));
        DietologistEmailSender sender = CreateSender(transport, templateProvider: templateProvider);
        var message = new DietologistInvitationMessage(
            "diet@example.com", Guid.NewGuid(), "token", "John", "Doe", "en");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Equal("dietologist_invitation", templateProvider.LastKey);
        Assert.Equal("en", templateProvider.LastLocale);
        Assert.Equal("Invite John Doe to FoodDiary", transport.LastSubject);
        Assert.Contains("John Doe", transport.LastHtmlBody, StringComparison.Ordinal);
        Assert.Contains("dietologist/accept", transport.LastHtmlBody, StringComparison.Ordinal);
    }

    private static DietologistEmailSender CreateSender(
        IEmailTransport transport,
        EmailOptions? options = null,
        IEmailTemplateProvider? templateProvider = null) =>
        new(options ?? DefaultOptions, templateProvider ?? new StubEmailTemplateProvider(), transport);

    private static DietologistInvitationMessage CreateMessage(
        string toEmail, string language = "en") =>
        new(toEmail, Guid.NewGuid(), "token-value", "Test", "User", language);

    [ExcludeFromCodeCoverage]
    private sealed class RecordingEmailTransport : IEmailTransport {
        public int SentCount { get; private set; }
        public string LastRecipient { get; private set; } = string.Empty;
        public string LastSubject { get; private set; } = string.Empty;
        public string LastHtmlBody { get; private set; } = string.Empty;

        public Task SendAsync(MailMessage message, CancellationToken cancellationToken) {
            SentCount++;
            LastRecipient = message.To[0].Address;
            LastSubject = message.Subject;
            LastHtmlBody = message.Body;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubEmailTemplateProvider(EmailTemplateContent? template = null) : IEmailTemplateProvider {
        public string LastKey { get; private set; } = string.Empty;
        public string LastLocale { get; private set; } = string.Empty;

        public Task<EmailTemplateContent?> GetActiveTemplateAsync(
            string key,
            string locale,
            CancellationToken cancellationToken = default) {
            LastKey = key;
            LastLocale = locale;
            return Task.FromResult(template);
        }
    }
}
