using FoodDiary.Email.Contracts.Email.Common;
using FoodDiary.Modules.Identity.Application.Email.Commands.QueueSubscriptionCancellationEmail;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Contracts.Email.Commands.QueueSubscriptionCancellationEmail;

namespace FoodDiary.Modules.Identity.Application.Tests;

public sealed partial class SubscriptionCancellationEmailTests {
    [Fact]
    public async Task EditableTemplate_PreservesUnknownTokensAndExpandsKnownValues() {
        var fixture = new Fixture();
        fixture.Templates.GetActiveTemplateAsync("subscription_cancellation", "en", Arg.Any<CancellationToken>())
            .Returns(new EmailTemplateContent("{{brand}} {{custom}}", "{{brand}} {{custom}}", "{{brand}} {{custom}}"));

        await fixture.Handler.Handle(new QueueSubscriptionCancellationEmailCommand("recipient@example.test", "en", "UTC", End, "test"), CancellationToken.None);

        EmailMessage message = Assert.Single(fixture.Messages);
        Assert.Multiple(
            () => Assert.Equal("FoodDiary {{custom}}", message.Subject),
            () => Assert.Equal("FoodDiary {{custom}}", message.HtmlBody),
            () => Assert.Equal("FoodDiary {{custom}}", message.TextBody));
    }

    [Theory]
    [InlineData("relative")]
    [InlineData("ftp://example.test")]
    public async Task InvalidFrontendOrigin_DoesNotEnqueueABrokenConfirmation(string origin) {
        var fixture = new Fixture();
        var handler = new QueueSubscriptionCancellationEmailCommandHandler(new EmailOptions {
            FrontendBaseUrl = origin,
            FromAddress = "noreply@example.test",
        }, fixture.Templates, fixture.Outbox);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new QueueSubscriptionCancellationEmailCommand("recipient@example.test", "en", "UTC", End, "test"), CancellationToken.None));

        Assert.Empty(fixture.Messages);
    }

    [Fact]
    public async Task UnspecifiedAccessEnd_DoesNotEnqueueAnAmbiguousPromise() {
        var fixture = new Fixture();

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Handler.Handle(
            new QueueSubscriptionCancellationEmailCommand("recipient@example.test", "en", "UTC", DateTime.SpecifyKind(End, DateTimeKind.Unspecified), "test"), CancellationToken.None));

        Assert.Empty(fixture.Messages);
    }
}
