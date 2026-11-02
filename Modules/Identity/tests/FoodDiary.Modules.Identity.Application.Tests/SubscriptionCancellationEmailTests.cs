using FoodDiary.Email.Contracts.Email.Common;
using FoodDiary.Modules.Identity.Application.Email.Commands.QueueSubscriptionCancellationEmail;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Contracts.Email.Commands.QueueSubscriptionCancellationEmail;

namespace FoodDiary.Modules.Identity.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed partial class SubscriptionCancellationEmailTests {
    private static readonly DateTime End = new(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RussianConfirmation_RendersEndInUserZoneAndProvidesReplyAddress() {
        var fixture = new Fixture();
        await fixture.Handler.Handle(new QueueSubscriptionCancellationEmailCommand("recipient@example.test", "ru", "Asia/Tbilisi", End, "cancellation:test"), CancellationToken.None);
        EmailMessage message = Assert.Single(fixture.Messages);
        Assert.Contains("Автопродление", message.Subject, StringComparison.Ordinal);
        Assert.Contains("14:00", message.TextBody, StringComparison.Ordinal);
        Assert.Contains("UTC+04:00", message.TextBody, StringComparison.Ordinal);
        Assert.Contains("feedback@fooddiary.club", message.TextBody, StringComparison.Ordinal);
        Assert.Contains("mailto:feedback@fooddiary.club", message.HtmlBody, StringComparison.Ordinal);
        Assert.Equal("feedback@fooddiary.club", message.ReplyTo);
        Assert.Equal("subscription_cancellation", message.Purpose);
        Assert.True(message.AutoSubmitted);
    }

    [Theory]
    [InlineData(6, "UTC+02:00", "12:00")]
    [InlineData(12, "UTC+01:00", "11:00")]
    public async Task Confirmation_RespectsDaylightSaving(int month, string offset, string hour) {
        var fixture = new Fixture();
        await fixture.Handler.Handle(new QueueSubscriptionCancellationEmailCommand("recipient@example.test", "en", "Europe/Berlin",
            new DateTime(2026, month, 1, 10, 0, 0, DateTimeKind.Utc), "test"), CancellationToken.None);
        Assert.Contains(offset, Assert.Single(fixture.Messages).TextBody, StringComparison.Ordinal);
        Assert.Contains(hour, fixture.Messages[0].TextBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidUserZone_FallsBackToExplicitUtc() {
        var fixture = new Fixture();
        await fixture.Handler.Handle(new QueueSubscriptionCancellationEmailCommand("recipient@example.test", "en", "invalid-zone", End, "test"), CancellationToken.None);
        Assert.Contains("UTC+00:00", Assert.Single(fixture.Messages).TextBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NonWholeHourZone_PreservesTheInstantAcrossTheLocalDayBoundary() {
        var fixture = new Fixture();
        await fixture.Handler.Handle(new QueueSubscriptionCancellationEmailCommand("recipient@example.test", "en", "Asia/Kathmandu",
            new DateTime(2026, 12, 1, 22, 30, 0, DateTimeKind.Utc), "test"), CancellationToken.None);
        string text = Assert.Single(fixture.Messages).TextBody!;
        Assert.Contains("UTC+05:45", text, StringComparison.Ordinal);
        Assert.Contains("4:15", text, StringComparison.Ordinal);
        Assert.Contains("12/2/2026", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompletedAccess_DoesNotPromiseAccessUntilAnotherDate() {
        var fixture = new Fixture();
        await fixture.Handler.Handle(new QueueSubscriptionCancellationEmailCommand("recipient@example.test", "en", "UTC", AccessUntilUtc: null, "test"), CancellationToken.None);
        string text = Assert.Single(fixture.Messages).TextBody!;
        Assert.Contains("has ended", text, StringComparison.Ordinal);
        Assert.DoesNotContain("continues until", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EditableTemplate_EscapesHtmlTokensAndKeepsPlainTextReadable() {
        var fixture = new Fixture("<Brand & Co>");
        fixture.Templates.GetActiveTemplateAsync("subscription_cancellation", "en", Arg.Any<CancellationToken>())
            .Returns(new EmailTemplateContent("{{brand}}: renewal off", "<p>{{brand}}</p>{{accessMessage}} <a href=\"{{feedbackLink}}\">{{feedbackEmail}}</a>", "{{brand}} {{accessMessage}} {{subscriptionLink}}"));
        await fixture.Handler.Handle(new QueueSubscriptionCancellationEmailCommand("recipient@example.test", "en", "UTC", End, "test"), CancellationToken.None);
        EmailMessage message = Assert.Single(fixture.Messages);
        Assert.Contains("&lt;Brand &amp; Co&gt;", message.HtmlBody, StringComparison.Ordinal);
        Assert.DoesNotContain("<Brand", message.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("<Brand & Co>", message.TextBody, StringComparison.Ordinal);
        Assert.Contains("https://fooddiary.example.test/premium", message.TextBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QueueFailure_PropagatesSoTheCallerRetainsItsIntent() {
        var fixture = new Fixture();
        fixture.Outbox.EnqueueAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Synthetic queue failure")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Handler.Handle(
            new QueueSubscriptionCancellationEmailCommand("recipient@example.test", "en", "UTC", End, "test"), CancellationToken.None));
    }

    private sealed class Fixture {
        public List<EmailMessage> Messages { get; } = [];
        public IEmailOutbox Outbox { get; } = Substitute.For<IEmailOutbox>();
        public IEmailTemplateProvider Templates { get; } = Substitute.For<IEmailTemplateProvider>();
        public QueueSubscriptionCancellationEmailCommandHandler Handler { get; }
        public Fixture(string brand = "FoodDiary") {
            Outbox.EnqueueAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>()).Returns(call => {
                Messages.Add(call.Arg<EmailMessage>());
                return Task.CompletedTask;
            });
            Handler = new QueueSubscriptionCancellationEmailCommandHandler(new EmailOptions {
                FromAddress = "noreply@example.test",
                FromName = brand,
                FrontendBaseUrl = "https://fooddiary.example.test",
            }, Templates, Outbox);
        }
    }
}
