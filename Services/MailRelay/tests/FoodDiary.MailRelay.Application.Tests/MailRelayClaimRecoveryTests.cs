using FoodDiary.MailRelay.Application.Abstractions;
using FoodDiary.MailRelay.Application.Emails.Services;
using FoodDiary.MailRelay.Application.Queue.Models;
using FoodDiary.MailRelay.Domain.Emails;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FoodDiary.MailRelay.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailRelayClaimRecoveryTests {
    [Fact]
    public async Task MissingInitialClaim_DoesNotReadRecipientsSendOrAcknowledge() {
        QueuedEmailMessage message = CreateMessage();
        IMailRelayQueueStore store = CreateStore();
        store.RenewClaimAsync(message.Id, message.AttemptCount, Arg.Any<CancellationToken>()).Returns(returnThis: false);
        IRelayDeliveryTransport transport = CreateTransport();

        MailRelayProcessResult result = await CreateProcessor(store, transport).ProcessAsync(message, CancellationToken.None);

        AssertRecoverable(result);
        await store.Received(1).RenewClaimAsync(message.Id, message.AttemptCount, CancellationToken.None);
        await store.DidNotReceive().GetSuppressedRecipientsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
        Assert.Empty(transport.ReceivedCalls());
        AssertNoCompletionOrRetry(store);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnershipLostDuringDeliveryOrAcknowledgement_DoesNotScheduleAStaleRetry(bool afterSmtpAccepted) {
        QueuedEmailMessage message = CreateMessage();
        IMailRelayQueueStore store = CreateStore();
        IRelayDeliveryTransport transport = CreateTransport();
        if (afterSmtpAccepted) {
            store.MarkSentAsync(message.Id, message.AttemptCount, Arg.Any<CancellationToken>())
                .Returns(Task.FromException(new MailRelayClaimLostException()));
        } else {
            transport.SendAsync(Arg.Any<RelayEmailMessageRequest>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException(new MailRelayClaimLostException()));
        }

        MailRelayProcessResult result = await CreateProcessor(store, transport).ProcessAsync(message, CancellationToken.None);

        AssertRecoverable(result);
        await transport.Received(1).SendAsync(Arg.Is<RelayEmailMessageRequest>(request =>
            request.MessageId == message.Id.Value.ToString("N") + "@mailrelay.invalid"), Arg.Any<CancellationToken>());
        await store.Received(afterSmtpAccepted ? 1 : 0).MarkSentAsync(message.Id, message.AttemptCount, Arg.Any<CancellationToken>());
        await store.DidNotReceive().MarkFailedAttemptAsync(Arg.Any<QueuedEmailFailureDecision>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().MarkSuppressedAsync(Arg.Any<QueuedEmailId>(), Arg.Any<int>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RenewalErrorOrTimeout_CancelsTheActiveSendWithoutExposingExceptionPayloadOrSchedulingRetry(bool timeout) {
        IMailRelayQueueStore store = CreateStore();
        store.ClaimRenewalInterval.Returns(TimeSpan.FromMilliseconds(20));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int renewals = 0;
        store.RenewClaimAsync(Arg.Any<QueuedEmailId>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => Interlocked.Increment(ref renewals) == 1
                ? Task.FromResult(true)
                : FailRenewalAsync(timeout, entered, call.Arg<CancellationToken>()));
        IRelayDeliveryTransport transport = CreateTransport();
        transport.SendAsync(Arg.Any<RelayEmailMessageRequest>(), Arg.Any<CancellationToken>()).Returns(async call => {
            CancellationToken token = call.Arg<CancellationToken>();
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), token).ConfigureAwait(false);
            await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false);
        });
        ILogger<MailRelayMessageProcessor> logger = Substitute.For<ILogger<MailRelayMessageProcessor>>();

        MailRelayProcessResult result = await CreateProcessor(store, transport, logger).ProcessAsync(CreateMessage(), CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(10));

        AssertRecoverable(result);
        Assert.True(entered.Task.IsCompletedSuccessfully);
        Assert.True(Volatile.Read(ref renewals) >= 2);
        AssertNoCompletionOrRetry(store);
        object?[][] logCalls = [.. logger.ReceivedCalls()
            .Where(call => string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal))
            .Select(call => call.GetArguments())];
        string messages = string.Join(Environment.NewLine, logCalls.Select(arguments => arguments[2]?.ToString()));
        Assert.Contains(timeout ? "TaskCanceledException" : "IOException", messages, StringComparison.Ordinal);
        Assert.DoesNotContain("recipient@example.test", messages, StringComparison.Ordinal);
        Assert.All(logCalls, arguments => Assert.Null(arguments[3]));
    }

    [Fact]
    public async Task CallerCancellationDuringRenewal_StopsBothOperationsAndPropagatesWithoutRetry() {
        IMailRelayQueueStore store = CreateStore();
        store.ClaimRenewalInterval.Returns(TimeSpan.FromMilliseconds(20));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var caller = new CancellationTokenSource();
        int renewals = 0;
        store.RenewClaimAsync(Arg.Any<QueuedEmailId>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => Interlocked.Increment(ref renewals) == 1
                ? Task.FromResult(true)
                : AwaitRenewalCancellationAsync(entered, caller, call.Arg<CancellationToken>()));
        IRelayDeliveryTransport transport = CreateTransport();
        transport.SendAsync(Arg.Any<RelayEmailMessageRequest>(), Arg.Any<CancellationToken>()).Returns(async call => {
            CancellationToken token = call.Arg<CancellationToken>();
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), token).ConfigureAwait(false);
            await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false);
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateProcessor(store, transport).ProcessAsync(CreateMessage(), caller.Token).WaitAsync(TimeSpan.FromSeconds(10)));

        Assert.True(caller.IsCancellationRequested);
        Assert.True(entered.Task.IsCompletedSuccessfully);
        AssertNoCompletionOrRetry(store);
    }

    private static async Task<bool> FailRenewalAsync(bool timeout, TaskCompletionSource entered, CancellationToken cancellationToken) {
        entered.TrySetResult();
        if (!timeout) { throw new IOException("Connection failed for recipient@example.test"); }
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        return false;
    }

    private static async Task<bool> AwaitRenewalCancellationAsync(TaskCompletionSource entered, CancellationTokenSource caller, CancellationToken cancellationToken) {
        entered.TrySetResult();
        await caller.CancelAsync().ConfigureAwait(false);
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        return false;
    }

    private static IMailRelayQueueStore CreateStore() {
        IMailRelayQueueStore store = Substitute.For<IMailRelayQueueStore>();
        store.ClaimRenewalInterval.Returns(TimeSpan.FromSeconds(30));
        store.RenewClaimAsync(Arg.Any<QueuedEmailId>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(returnThis: true);
        store.GetSuppressedRecipientsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([]));
        return store;
    }

    private static IRelayDeliveryTransport CreateTransport() {
        IRelayDeliveryTransport transport = Substitute.For<IRelayDeliveryTransport>();
        transport.SendAsync(Arg.Any<RelayEmailMessageRequest>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        return transport;
    }

    private static MailRelayMessageProcessor CreateProcessor(IMailRelayQueueStore store, IRelayDeliveryTransport transport,
        ILogger<MailRelayMessageProcessor>? logger = null) => new(store, new SmtpSubmissionService(transport),
        logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MailRelayMessageProcessor>.Instance);

    private static QueuedEmailMessage CreateMessage() => new(QueuedEmailId.New(), "relay@example.test", "FoodDiary", ["recipient@example.test"],
        "Subject", "<p>Body</p>", "Body", "correlation", AttemptCount: 2, MaxAttempts: 3);

    private static void AssertRecoverable(MailRelayProcessResult result) => Assert.Multiple(
        () => Assert.False(result.Succeeded),
        () => Assert.False(result.IsTerminalFailure),
        () => Assert.Null(result.RetryAvailableAtUtc));

    private static void AssertNoCompletionOrRetry(IMailRelayQueueStore store) {
        Assert.DoesNotContain(store.ReceivedCalls(), call => call.GetMethodInfo().Name is
            nameof(IMailRelayQueueStore.MarkSentAsync) or nameof(IMailRelayQueueStore.MarkSuppressedAsync) or nameof(IMailRelayQueueStore.MarkFailedAttemptAsync));
    }
}
