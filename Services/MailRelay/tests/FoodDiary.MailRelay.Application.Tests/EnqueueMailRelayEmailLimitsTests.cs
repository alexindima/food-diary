using FoodDiary.MailRelay.Application.Abstractions;
using FoodDiary.MailRelay.Application.Emails.Commands.EnqueueMailRelayEmail;
using FoodDiary.MailRelay.Application.Emails.Services;
using FoodDiary.MailRelay.Domain.Emails;
using NSubstitute;
using FoodDiary.Results;

namespace FoodDiary.MailRelay.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class EnqueueMailRelayEmailLimitsTests {
    [Theory]
    [InlineData("recipients")]
    [InlineData("subject")]
    [InlineData("html")]
    [InlineData("combined")]
    [InlineData("unicode")]
    [InlineData("identifier")]
    [InlineData("name")]
    public async Task EnqueueAsync_WithOversizedRequest_RejectsBeforeQueueOrNotification(string oversizedField) {
        RelayEmailMessageRequest request = ValidRequest();
        request = oversizedField switch {
            "recipients" => request with { To = Enumerable.Repeat("user@example.com", RelayEmailMessageLimits.MaximumRecipients + 1).ToArray() },
            "subject" => request with { Subject = new string('x', RelayEmailMessageLimits.MaximumSubjectLength + 1) },
            "html" => request with { HtmlBody = new string('x', RelayEmailMessageLimits.MaximumBodyBytes + 1) },
            "combined" => request with { HtmlBody = new string('x', RelayEmailMessageLimits.MaximumBodyBytes / 2), TextBody = new string('x', (RelayEmailMessageLimits.MaximumBodyBytes / 2) + 1) },
            "unicode" => request with { HtmlBody = new string('\u0416', (RelayEmailMessageLimits.MaximumBodyBytes / 2) + 1) },
            "identifier" => request with { IdempotencyKey = new string('x', RelayEmailMessageLimits.MaximumIdentifierLength + 1) },
            _ => request with { FromName = new string('x', RelayEmailMessageLimits.MaximumNameLength + 1) },
        };
        IMailRelayQueueStore queue = Substitute.For<IMailRelayQueueStore>();
        IMailRelayDispatchNotifier notifier = Substitute.For<IMailRelayDispatchNotifier>();
        IMailRelayDeliveryPolicy policy = Substitute.For<IMailRelayDeliveryPolicy>();
        var useCases = new MailRelayEmailUseCases(queue, notifier, policy);

        Result<Guid> result = await useCases.EnqueueAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.False((await new EnqueueMailRelayEmailCommandValidator().ValidateAsync(new EnqueueMailRelayEmailCommand(request), CancellationToken.None)).IsValid);
        await queue.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default);
        await notifier.DidNotReceiveWithAnyArgs().NotifyQueuedAsync(default, default);
    }

    [Fact]
    public async Task EnqueueAsync_WithBoundarySizedMessage_PreservesDelivery() {
        RelayEmailMessageRequest request = ValidRequest() with {
            To = Enumerable.Repeat("user@example.com", RelayEmailMessageLimits.MaximumRecipients).ToArray(),
            HtmlBody = new string('x', RelayEmailMessageLimits.MaximumBodyBytes),
            TextBody = null,
        };
        IMailRelayQueueStore queue = Substitute.For<IMailRelayQueueStore>();
        IMailRelayDispatchNotifier notifier = Substitute.For<IMailRelayDispatchNotifier>();
        var id = Guid.NewGuid();
        queue.EnqueueAsync(request, CancellationToken.None).Returns(id);
        var useCases = new MailRelayEmailUseCases(queue, notifier, new NoOpMailRelayDeliveryPolicy());

        Result<Guid> result = await useCases.EnqueueAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value);
        Assert.True((await new EnqueueMailRelayEmailCommandValidator().ValidateAsync(new EnqueueMailRelayEmailCommand(request), CancellationToken.None)).IsValid);
        await notifier.Received(1).NotifyQueuedAsync(id, CancellationToken.None);
    }

    private static RelayEmailMessageRequest ValidRequest() => new("noreply@example.com", "FoodDiary", ["user@example.com"], "Invitation", "<p>Hello</p>", "Hello");
}
