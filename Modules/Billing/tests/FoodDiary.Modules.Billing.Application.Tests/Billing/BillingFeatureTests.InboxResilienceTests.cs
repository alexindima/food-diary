using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfileIncludingDeleted;
using System.Text.Json;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;
using FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhookInbox;
using FoodDiary.Modules.Billing.Application.Commands.ProcessQueuedBillingWebhook;
using FoodDiary.Modules.Billing.Application.Services;
using FoodDiary.Modules.Billing.Contracts.Commands.ProcessBillingWebhookInbox;
using FoodDiary.Modules.Billing.Contracts.Models;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Results;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

public partial class BillingFeatureTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Inbox_RecordsFailureAndContinuesBatch_UnlessCanceled(bool canceled) {
        User firstUser = CreatePremiumUser("failed-inbox@example.com");
        User secondUser = CreatePremiumUser("healthy-inbox@example.com");
        var events = new RecordingBillingWebhookEventRepository();
        BillingWebhookEvent first = CreateReceivedEvent(CreateWebhookPaymentEvent(firstUser, "evt_fail", "pay_fail"));
        BillingWebhookEvent second = CreateReceivedEvent(CreateWebhookPaymentEvent(secondUser, "evt_ok", "pay_ok"));
        events.Events.AddRange([first, second]);
        ISender users = Substitute.For<ISender>();
        users.Send(new GetUserBillingProfileIncludingDeletedQuery(UserId: firstUser.Id), Arg.Any<CancellationToken>())
            .Returns<Task<UserBillingProfileModel?>>(_ => throw (canceled
                ? new OperationCanceledException("Interrupted")
                : new InvalidOperationException("sensitive provider payload")));
        users.Send(new GetUserBillingProfileIncludingDeletedQuery(UserId: secondUser.Id), Arg.Any<CancellationToken>())
            .Returns(CreateBillingProfile(secondUser));
        ProcessQueuedBillingWebhookCommandHandler handler = CreateResilientInboxHandler(events, users);
        await using ServiceProvider provider = new ServiceCollection().AddFoodDiaryMediator(_ => { })
            .AddSingleton<IRequestHandler<ProcessQueuedBillingWebhookCommand, Result>>(handler).BuildServiceProvider();
        var batch = new ProcessBillingWebhookInboxCommandHandler(events, provider.GetRequiredService<ISender>());
        var command = new ProcessBillingWebhookInboxCommand(10);

        if (canceled) {
            await Assert.ThrowsAsync<OperationCanceledException>(() => batch.Handle(command, CancellationToken.None));
            Assert.Multiple(
                () => Assert.Equal(0, first.AttemptCount),
                () => Assert.Equal(BillingWebhookEvent.ReceivedStatus, first.Status),
                () => Assert.Equal(BillingWebhookEvent.ReceivedStatus, second.Status));
            await users.DidNotReceive().Send(new GetUserBillingProfileIncludingDeletedQuery(UserId: secondUser.Id), Arg.Any<CancellationToken>());
        } else {
            BillingWebhookInboxRunResult result = await batch.Handle(command, CancellationToken.None);
            Assert.Multiple(
                () => Assert.Equal(1, result.Processed),
                () => Assert.Equal(1, result.Failed),
                () => Assert.Equal(1, first.AttemptCount),
                () => Assert.Equal(Now.AddMinutes(1), first.NextAttemptAtUtc),
                () => Assert.Equal(BillingErrors.WebhookProcessingFailed.Message, first.ErrorMessage),
                () => Assert.Equal(BillingWebhookEvent.FailedStatus, first.Status),
                () => Assert.Equal(BillingWebhookEvent.ProcessedStatus, second.Status));
        }
    }

    [Fact]
    public async Task Inbox_FailureBookkeepingError_IsPropagated() {
        User user = CreatePremiumUser("bookkeeping-inbox@example.com");
        BillingWebhookEvent inbox = CreateReceivedEvent(CreateWebhookPaymentEvent(user, "evt_bookkeeping", "pay_bookkeeping"));
        IBillingWebhookEventWriteRepository events = Substitute.For<IBillingWebhookEventWriteRepository>();
        events.GetByIdAsync(inbox.Id, Arg.Any<CancellationToken>()).Returns(inbox);
        var failure = new InvalidOperationException("Cannot persist retry");
        events.UpdateAsync(inbox, Arg.Any<CancellationToken>()).Returns<Task>(_ => throw failure);
        ISender users = Substitute.For<ISender>();
        users.Send(new GetUserBillingProfileIncludingDeletedQuery(UserId: user.Id), Arg.Any<CancellationToken>())
            .Returns<Task<UserBillingProfileModel?>>(_ => throw new InvalidOperationException("Processing failed"));
        ProcessQueuedBillingWebhookCommandHandler handler = CreateResilientInboxHandler(events, users);

        Exception actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new ProcessQueuedBillingWebhookCommand(inbox.Id), CancellationToken.None));

        Assert.Same(failure, actual);
    }

    [Fact]
    public async Task Inbox_ConcurrentCompletion_IsNotReplacedByFailure() {
        User user = CreatePremiumUser("concurrent-inbox@example.com");
        BillingWebhookEvent inbox = CreateReceivedEvent(CreateWebhookPaymentEvent(user, "evt_concurrent", "pay_concurrent"));
        IBillingWebhookEventWriteRepository events = Substitute.For<IBillingWebhookEventWriteRepository>();
        events.GetByIdAsync(inbox.Id, Arg.Any<CancellationToken>()).Returns(inbox);
        ISender users = Substitute.For<ISender>();
        users.Send(new GetUserBillingProfileIncludingDeletedQuery(UserId: user.Id), Arg.Any<CancellationToken>())
            .Returns<Task<UserBillingProfileModel?>>(_ => {
                inbox.MarkProcessed(Now);
                throw new InvalidOperationException("Another worker completed the event");
            });
        ProcessQueuedBillingWebhookCommandHandler handler = CreateResilientInboxHandler(events, users);

        Result result = await handler.Handle(new ProcessQueuedBillingWebhookCommand(inbox.Id), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(0, inbox.AttemptCount);
        await events.DidNotReceive().UpdateAsync(Arg.Any<BillingWebhookEvent>(), Arg.Any<CancellationToken>());
    }

    private static BillingWebhookEvent CreateReceivedEvent(BillingWebhookEventModel model) =>
        BillingWebhookEvent.CreateReceived(BillingProviderNames.YooKassa, model.EventId, model.EventType,
            model.ExternalSubscriptionId, Now, "{}", JsonSerializer.Serialize(model));

    private static ProcessQueuedBillingWebhookCommandHandler CreateResilientInboxHandler(
        IBillingWebhookEventWriteRepository events, ISender users) {
        var subscriptions = new InMemoryBillingSubscriptionRepository();
        var transactions = new NoOpBillingTransactionRunner();
        var clock = new FixedDateTimeProvider(Now);
        var resolver = new BillingWebhookContextResolver(subscriptions, users);
        var processor = new BillingWebhookEventProcessor(events, transactions, resolver,
            new BillingWebhookSubscriptionWriter(subscriptions, clock),
            new BillingWebhookPaymentRecorder(new RecordingBillingPaymentRepository()),
            new BillingWebhookPremiumRoleSyncer(subscriptions, new BillingAccessService(users, subscriptions, clock),
                new NoOpMarketingConversionRecorder(), clock), clock);
        return new ProcessQueuedBillingWebhookCommandHandler(events, transactions, processor, clock, resolver);
    }
}
