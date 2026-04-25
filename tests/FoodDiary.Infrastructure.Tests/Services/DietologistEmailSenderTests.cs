using System.Net.Mail;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Services;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class DietologistEmailSenderTests {
    private static readonly EmailOptions DefaultOptions = new() {
        FromAddress = "noreply@fooddiary.club",
        FromName = "FoodDiary",
        FrontendBaseUrl = "https://fooddiary.club",
    };

    [Fact]
    public async Task SendDietologistInvitationAsync_SendsEmailToRecipient() {
        var transport = new RecordingEmailTransport();
        var sender = CreateSender(transport);
        var message = CreateMessage("diet@example.com");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Equal(1, transport.SentCount);
        Assert.Equal("diet@example.com", transport.LastRecipient);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_WithEnglishLocale_HasEnglishSubject() {
        var transport = new RecordingEmailTransport();
        var sender = CreateSender(transport);
        var message = CreateMessage("diet@example.com", language: "en");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Contains("Invitation", transport.LastSubject);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_WithRussianLocale_HasRussianSubject() {
        var transport = new RecordingEmailTransport();
        var sender = CreateSender(transport);
        var message = CreateMessage("diet@example.com", language: "ru");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Contains("Приглашение", transport.LastSubject);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_IncludesInvitationLink() {
        var transport = new RecordingEmailTransport();
        var sender = CreateSender(transport);
        var invitationId = Guid.NewGuid();
        var message = new DietologistInvitationMessage(
            "diet@example.com", invitationId, "test-token", "John", "Doe", "en");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Contains("dietologist/accept", transport.LastHtmlBody);
        Assert.Contains(invitationId.ToString(), transport.LastHtmlBody);
        Assert.Contains("test-token", transport.LastHtmlBody);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_IncludesClientName() {
        var transport = new RecordingEmailTransport();
        var sender = CreateSender(transport);
        var message = new DietologistInvitationMessage(
            "diet@example.com", Guid.NewGuid(), "token", "Алексей", "Иванов", "ru");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Contains("Алексей Иванов", transport.LastHtmlBody);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_WithNullClientName_UsesDefaultName() {
        var transport = new RecordingEmailTransport();
        var sender = CreateSender(transport);
        var message = new DietologistInvitationMessage(
            "diet@example.com", Guid.NewGuid(), "token", null, null, "en");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Contains("A user", transport.LastHtmlBody);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_WithNullLanguage_DefaultsToEnglish() {
        var transport = new RecordingEmailTransport();
        var sender = CreateSender(transport);
        var message = new DietologistInvitationMessage(
            "diet@example.com", Guid.NewGuid(), "token", "John", null, null);

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Contains("Invitation", transport.LastSubject);
    }

    private static DietologistEmailSender CreateSender(
        IEmailTransport transport, EmailOptions? options = null) =>
        new(Microsoft.Extensions.Options.Options.Create(options ?? DefaultOptions), transport);

    private static DietologistInvitationMessage CreateMessage(
        string toEmail, string language = "en") =>
        new(toEmail, Guid.NewGuid(), "token-value", "Test", "User", language);

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
}
