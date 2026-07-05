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
        IEmailOutbox outbox = CreateCapturingOutbox(out Func<(int Count, string Recipient, string Subject, string HtmlBody)> getSent);
        DietologistEmailSender sender = CreateSender(outbox);
        DietologistInvitationMessage message = CreateMessage("diet@example.com");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Equal(1, getSent().Count);
        Assert.Equal("diet@example.com", getSent().Recipient);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_WithEnglishLocale_HasEnglishSubject() {
        IEmailOutbox outbox = CreateCapturingOutbox(out Func<(int Count, string Recipient, string Subject, string HtmlBody)> getSent);
        DietologistEmailSender sender = CreateSender(outbox);
        DietologistInvitationMessage message = CreateMessage("diet@example.com", language: "en");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Contains("Invitation", getSent().Subject, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_WithRussianLocale_HasRussianSubject() {
        IEmailOutbox outbox = CreateCapturingOutbox(out Func<(int Count, string Recipient, string Subject, string HtmlBody)> getSent);
        DietologistEmailSender sender = CreateSender(outbox);
        DietologistInvitationMessage message = CreateMessage("diet@example.com", language: "ru");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Contains("\u041f\u0440\u0438\u0433\u043b\u0430\u0448\u0435\u043d\u0438\u0435", getSent().Subject, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_IncludesInvitationLink() {
        IEmailOutbox outbox = CreateCapturingOutbox(out Func<(int Count, string Recipient, string Subject, string HtmlBody)> getSent);
        DietologistEmailSender sender = CreateSender(outbox);
        var invitationId = Guid.NewGuid();
        var message = new DietologistInvitationMessage(
            "diet@example.com", invitationId, "test-token", "John", "Doe", "en");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Contains("dietologist/accept", getSent().HtmlBody, StringComparison.Ordinal);
        Assert.Contains(invitationId.ToString(), getSent().HtmlBody, StringComparison.Ordinal);
        Assert.Contains("test-token", getSent().HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_IncludesClientName() {
        IEmailOutbox outbox = CreateCapturingOutbox(out Func<(int Count, string Recipient, string Subject, string HtmlBody)> getSent);
        DietologistEmailSender sender = CreateSender(outbox);
        var message = new DietologistInvitationMessage(
            "diet@example.com", Guid.NewGuid(), "token", "\u0410\u043b\u0435\u043a\u0441\u0435\u0439", "\u0418\u0432\u0430\u043d\u043e\u0432", "ru");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Contains("\u0410\u043b\u0435\u043a\u0441\u0435\u0439 \u0418\u0432\u0430\u043d\u043e\u0432", getSent().HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_WithNullClientName_UsesDefaultName() {
        IEmailOutbox outbox = CreateCapturingOutbox(out Func<(int Count, string Recipient, string Subject, string HtmlBody)> getSent);
        DietologistEmailSender sender = CreateSender(outbox);
        var message = new DietologistInvitationMessage(
            "diet@example.com", Guid.NewGuid(), "token", ClientFirstName: null, ClientLastName: null, "en");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Contains("A user", getSent().HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_WithNullLanguage_DefaultsToEnglish() {
        IEmailOutbox outbox = CreateCapturingOutbox(out Func<(int Count, string Recipient, string Subject, string HtmlBody)> getSent);
        DietologistEmailSender sender = CreateSender(outbox);
        var message = new DietologistInvitationMessage(
            "diet@example.com", Guid.NewGuid(), "token", "John", ClientLastName: null, Language: null);

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Contains("Invitation", getSent().Subject, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendDietologistInvitationAsync_WithStoredTemplate_UsesTemplateTokens() {
        IEmailOutbox outbox = CreateCapturingOutbox(out Func<(int Count, string Recipient, string Subject, string HtmlBody)> getSent);
        string lastKey = string.Empty;
        string lastLocale = string.Empty;
        IEmailTemplateProvider templateProvider = CreateTemplateProvider(new EmailTemplateContent(
            "Invite {{clientName}} to {{brand}}",
            "<p>{{clientName}}</p><a href=\"{{link}}\">{{brand}}</a>",
            "{{clientName}} {{link}} {{brand}}"),
            key => lastKey = key,
            locale => lastLocale = locale);
        DietologistEmailSender sender = CreateSender(outbox, templateProvider: templateProvider);
        var message = new DietologistInvitationMessage(
            "diet@example.com", Guid.NewGuid(), "token", "John", "Doe", "en");

        await sender.SendDietologistInvitationAsync(message, CancellationToken.None);

        Assert.Equal("dietologist_invitation", lastKey);
        Assert.Equal("en", lastLocale);
        Assert.Equal("Invite John Doe to FoodDiary", getSent().Subject);
        Assert.Contains("John Doe", getSent().HtmlBody, StringComparison.Ordinal);
        Assert.Contains("dietologist/accept", getSent().HtmlBody, StringComparison.Ordinal);
    }

    private static DietologistEmailSender CreateSender(
        IEmailOutbox outbox,
        EmailOptions? options = null,
        IEmailTemplateProvider? templateProvider = null) =>
        new(options ?? DefaultOptions, templateProvider ?? CreateTemplateProvider(), outbox);

    private static DietologistInvitationMessage CreateMessage(
        string toEmail, string language = "en") =>
        new(toEmail, Guid.NewGuid(), "token-value", "Test", "User", language);

    private static IEmailOutbox CreateCapturingOutbox(
        out Func<(int Count, string Recipient, string Subject, string HtmlBody)> getSent) {
        IEmailOutbox outbox = Substitute.For<IEmailOutbox>();
        (int Count, string Recipient, string Subject, string HtmlBody) sent = (0, string.Empty, string.Empty, string.Empty);
        outbox
            .EnqueueAsync(Arg.Do<EmailMessage>(message => {
                sent = (sent.Count + 1, message.ToAddresses[0], message.Subject, message.HtmlBody);
            }), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        getSent = () => sent;
        return outbox;
    }

    private static IEmailTemplateProvider CreateTemplateProvider(
        EmailTemplateContent? template = null,
        Action<string>? captureKey = null,
        Action<string>? captureLocale = null) {
        IEmailTemplateProvider templateProvider = Substitute.For<IEmailTemplateProvider>();
        templateProvider
            .GetActiveTemplateAsync(
                Arg.Do<string>(key => captureKey?.Invoke(key)),
                Arg.Do<string>(locale => captureLocale?.Invoke(locale)),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(template));
        return templateProvider;
    }
}
