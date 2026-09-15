using FoodDiary.Mediator;
using FoodDiary.Modules.Marketing.Contracts.Commands.RecordPremiumConversion;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfileIncludingDeleted;
using System.Text.Json;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;
using FoodDiary.Modules.Billing.Application.Commands.ProcessQueuedBillingWebhook;
using FoodDiary.Modules.Billing.Application.Commands.RenewDueSubscriptions;
using FoodDiary.Modules.Billing.Application.Services;
using FoodDiary.Modules.Billing.Contracts.Commands.RenewDueSubscriptions;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class SharedBillingContextIntegrationTests {
    [RequiresDockerFact]
    public async Task InboxFailure_RollsBackBusinessWrites_PersistsBackoff_AndAllowsNextEventAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("inbox-rollback@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(context);
        IBillingTransactionRunner runner = provider.GetRequiredService<IBillingTransactionRunner>();
        IBillingWebhookEventWriteRepository events = provider.GetRequiredService<IBillingWebhookEventWriteRepository>();
        DateTime now = DateTime.UtcNow;
        BillingWebhookEventModel failedModel = WorkflowWebhook(user, "failed", "payment_failed", now);
        BillingWebhookEventModel healthyModel = WorkflowWebhook(user, "healthy", "payment_healthy", now);
        var failed = BillingWebhookEvent.CreateReceived(BillingProviderNames.YooKassa, failedModel.EventId,
            failedModel.EventType, failedModel.ExternalSubscriptionId, now, "{}", JsonSerializer.Serialize(failedModel));
        var healthy = BillingWebhookEvent.CreateReceived(BillingProviderNames.YooKassa, healthyModel.EventId,
            healthyModel.EventType, healthyModel.ExternalSubscriptionId, now, "{}", JsonSerializer.Serialize(healthyModel));
        await runner.ExecuteAsync(async token => {
            await events.AddAsync(failed, token);
            await events.AddAsync(healthy, token);
        });
        ISender users = WorkflowUsers(user);
        ISender marketing = Substitute.For<ISender>();
        int attempts = 0;
        marketing.Send(new RecordPremiumConversionCommand(UserId: user.Id.Value), Arg.Any<CancellationToken>()).Returns(_ => {
            if (++attempts == 1) {
                throw new InvalidOperationException("Sensitive provider failure after subscription mutation");
            }
            return Task.FromResult(Unit.Value);
        });
        var resolver = new BillingWebhookContextResolver(provider.GetRequiredService<IBillingSubscriptionWriteRepository>(), users);
        var handler = new ProcessQueuedBillingWebhookCommandHandler(events, runner,
            WorkflowProcessor(provider, users, marketing), TimeProvider.System, resolver);

        Result failedResult = await handler.Handle(new ProcessQueuedBillingWebhookCommand(failed.Id), CancellationToken.None);
        Assert.True(failedResult.IsFailure);
        await using FoodDiaryDbContext verification = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        Assert.Empty(await verification.BillingSubscriptions.AsNoTracking().ToListAsync());
        Assert.Empty(await verification.BillingPayments.AsNoTracking().ToListAsync());
        BillingWebhookEvent storedFailure = await verification.BillingWebhookEvents.AsNoTracking().SingleAsync(item => item.Id == failed.Id);
        Assert.Multiple(
            () => Assert.Equal(1, storedFailure.AttemptCount),
            () => Assert.Equal(BillingWebhookEvent.FailedStatus, storedFailure.Status),
            () => Assert.Equal(BillingErrors.WebhookProcessingFailed.Message, storedFailure.ErrorMessage),
            () => Assert.True(storedFailure.NextAttemptAtUtc > now));

        Result nextResult = await handler.Handle(new ProcessQueuedBillingWebhookCommand(healthy.Id), CancellationToken.None);

        Assert.True(nextResult.IsSuccess);
        Assert.Equal("healthy", (await verification.BillingSubscriptions.AsNoTracking().SingleAsync()).LastWebhookEventId);
        Assert.Single(await verification.BillingPayments.AsNoTracking().ToListAsync());
        Assert.Equal(BillingWebhookEvent.ProcessedStatus,
            (await verification.BillingWebhookEvents.AsNoTracking().SingleAsync(item => item.Id == healthy.Id)).Status);
    }

    [RequiresDockerFact]
    public async Task RenewalResponse_AfterWebhookInAnotherScope_PreservesCommittedWebhookAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("renewal-webhook-race@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(context);
        DateTime now = DateTime.UtcNow;
        IBillingTransactionRunner runner = provider.GetRequiredService<IBillingTransactionRunner>();
        IBillingSubscriptionWriteRepository subscriptions = provider.GetRequiredService<IBillingSubscriptionWriteRepository>();
        var initial = BillingSubscription.CreatePending(user.Id, BillingProviderNames.YooKassa, user.Id.Value.ToString(), "price", "monthly");
        initial.ApplyProviderSnapshot(BillingProviderNames.YooKassa, "initial", "pm_saved", "price", "monthly", "active",
            now.AddMonths(-1), now.AddMinutes(-1), cancelAtPeriodEnd: false, canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null,
            "initial", now.AddMonths(-1), providerMetadataJson: null, now.AddMonths(-1));
        await runner.ExecuteAsync(async token => await subscriptions.AddAsync(initial, token));
        await using FoodDiaryDbContext otherContext = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        await using ServiceProvider otherProvider = CreateProvider(otherContext);
        ISender users = WorkflowUsers(user);
        IBillingRecurringProviderGateway gateway = Substitute.For<IBillingRecurringProviderGateway>();
        gateway.Provider.Returns(BillingProviderNames.YooKassa);
        gateway.CreateRecurringPaymentAsync(Arg.Any<BillingRecurringPaymentRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(async _ => {
                Result webhookResult = await WorkflowProcessor(otherProvider, users, Substitute.For<ISender>())
                    .ProcessAsync(BillingProviderNames.YooKassa, "{}",
                        WorkflowWebhook(user, "newer", "payment_newer", now) with { CancelAtPeriodEnd = true },
                        inboxEvent: null, CancellationToken.None);
                Assert.True(webhookResult.IsSuccess);
                return Result.Success(new BillingRecurringPaymentModel("payment_old", "pm_saved", "price", "monthly", "active",
                    now, now.AddMonths(1), "old_response", 299m, "RUB", ProviderMetadataJson: null));
            });
        IBillingPaymentWriteRepository payments = provider.GetRequiredService<IBillingPaymentWriteRepository>();
        var renewal = new RenewDueSubscriptionsCommandHandler(subscriptions, payments, users, runner, [gateway],
            new BillingAccessService(users, subscriptions, TimeProvider.System), TimeProvider.System);

        await renewal.Handle(new RenewDueSubscriptionsCommand(BillingProviderNames.YooKassa, 10), CancellationToken.None);

        await using FoodDiaryDbContext verification = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        BillingSubscription stored = await verification.BillingSubscriptions.AsNoTracking().SingleAsync();
        Assert.Multiple(
            () => Assert.Equal("newer", stored.LastWebhookEventId),
            () => Assert.Equal("payment_newer", stored.ExternalSubscriptionId),
            () => Assert.True(stored.CancelAtPeriodEnd));
        Assert.Equal(2, await verification.BillingPayments.CountAsync());
        await gateway.Received(1).CreateRecurringPaymentAsync(Arg.Any<BillingRecurringPaymentRequestModel>(), Arg.Any<CancellationToken>());
    }

    [RequiresDockerTheory]
    [InlineData(true, "active")]
    [InlineData(false, "active")]
    [InlineData(true, "canceled")]
    [InlineData(false, "canceled")]
    public async Task RenewalAndWebhook_InSeparateScopes_ConvergeAsync(bool webhookFirst, string status) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("renewal-consistency@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(context);
        // PostgreSQL stores microseconds; use whole seconds for exact cursor/period assertions.
        DateTime now = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).UtcDateTime;
        DateTime oldEnd = now.AddDays(-1);
        IBillingTransactionRunner runner = provider.GetRequiredService<IBillingTransactionRunner>();
        IBillingSubscriptionWriteRepository subscriptions = provider.GetRequiredService<IBillingSubscriptionWriteRepository>();
        var initial = BillingSubscription.CreatePending(user.Id, BillingProviderNames.YooKassa, user.Id.Value.ToString(), "price", "monthly");
        initial.ApplyProviderSnapshot(BillingProviderNames.YooKassa, "initial", "pm_saved", "price", "monthly", "active",
            oldEnd.AddMonths(-1), oldEnd, cancelAtPeriodEnd: false, canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null,
            "initial", oldEnd.AddMonths(-1), webhookOccurredAtUtc: oldEnd.AddMonths(-1));
        await runner.ExecuteAsync(async token => await subscriptions.AddAsync(initial, token));
        await using FoodDiaryDbContext otherContext = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        await using ServiceProvider otherProvider = CreateProvider(otherContext);
        ISender users = WorkflowUsers(user);
        BillingWebhookEventProcessor processor = WorkflowProcessor(otherProvider, users, Substitute.For<ISender>());
        BillingWebhookEventModel webhook = WorkflowWebhook(user, "webhook_final", "payment_final", now) with {
            Status = status,
            IsRenewal = true,
            CurrentPeriodStartUtc = null,
            CurrentPeriodEndUtc = null,
        };
        IBillingRecurringProviderGateway gateway = Substitute.For<IBillingRecurringProviderGateway>();
        gateway.Provider.Returns(BillingProviderNames.YooKassa);
        gateway.CreateRecurringPaymentAsync(Arg.Any<BillingRecurringPaymentRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(async _ => {
                if (webhookFirst) {
                    Assert.True((await processor.ProcessAsync(BillingProviderNames.YooKassa, "{}", webhook, inboxEvent: null, CancellationToken.None)).IsSuccess);
                }
                return Result.Success(new BillingRecurringPaymentModel("payment_final", "pm_saved", "price", "monthly", status,
                    string.Equals(status, "active", StringComparison.Ordinal) ? oldEnd : null,
                    string.Equals(status, "active", StringComparison.Ordinal) ? oldEnd.AddMonths(1) : oldEnd,
                    "response_final", 299m, "RUB", ProviderMetadataJson: null, OccurredAtUtc: now));
            });
        var renewal = new RenewDueSubscriptionsCommandHandler(subscriptions, provider.GetRequiredService<IBillingPaymentWriteRepository>(),
            users, runner, [gateway], new BillingAccessService(users, subscriptions, TimeProvider.System), TimeProvider.System);

        await renewal.Handle(new RenewDueSubscriptionsCommand(BillingProviderNames.YooKassa, 10), CancellationToken.None);
        if (!webhookFirst) {
            Assert.True((await processor.ProcessAsync(BillingProviderNames.YooKassa, "{}", webhook, inboxEvent: null, CancellationToken.None)).IsSuccess);
        }

        await using FoodDiaryDbContext verification = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        BillingSubscription stored = await verification.BillingSubscriptions.AsNoTracking().SingleAsync();
        BillingPayment payment = await verification.BillingPayments.AsNoTracking().SingleAsync();
        bool paid = string.Equals(status, "active", StringComparison.Ordinal);
        Assert.Multiple(
            () => Assert.Equal(paid ? "active" : "past_due", stored.Status),
            () => Assert.Equal(paid ? oldEnd.AddMonths(1) : oldEnd, stored.CurrentPeriodEndUtc),
            () => Assert.Equal(now, stored.LastWebhookOccurredAtUtc),
            () => Assert.Equal(status, payment.Status),
            () => Assert.Equal(BillingPaymentKinds.Renewal, payment.Kind),
            () => Assert.Equal(now, payment.OccurredAtUtc));
        if (!paid) {
            Assert.True(stored.NextBillingAttemptUtc >= now.AddHours(1));
            Assert.Null(payment.CurrentPeriodStartUtc);
        }
    }

    [RequiresDockerFact]
    public async Task RenewalBatch_ReloadsSecondSubscriptionAfterWebhookInAnotherScopeAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var first = User.Create("first-batch@example.com", "hash");
        var second = User.Create("second-batch@example.com", "hash");
        context.Users.AddRange(first, second);
        await context.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(context);
        DateTime now = DateTime.UtcNow;
        IBillingTransactionRunner runner = provider.GetRequiredService<IBillingTransactionRunner>();
        IBillingSubscriptionWriteRepository subscriptions = provider.GetRequiredService<IBillingSubscriptionWriteRepository>();
        foreach (User user in new[] { first, second }) {
            var subscription = BillingSubscription.CreatePending(user.Id, BillingProviderNames.YooKassa, user.Id.Value.ToString(), "price", "monthly");
            subscription.ApplyProviderSnapshot(BillingProviderNames.YooKassa, user.Id.Value.ToString(), $"pm_{user.Id.Value:N}", "price", "monthly", "active",
                now.AddMonths(-1), now.AddDays(user == first ? -2 : -1), cancelAtPeriodEnd: false, canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null,
                user.Id.Value.ToString(), now.AddMonths(-1), webhookOccurredAtUtc: now.AddMonths(-1));
            await runner.ExecuteAsync(async token => await subscriptions.AddAsync(subscription, token));
        }
        await using FoodDiaryDbContext otherContext = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        await using ServiceProvider otherProvider = CreateProvider(otherContext);
        ISender users = WorkflowUsers(first);
        users.Send(new GetUserBillingProfileIncludingDeletedQuery(UserId: second.Id), Arg.Any<CancellationToken>()).Returns(new UserBillingProfileModel(
            second.Id, second.Email, IsActive: true, IsDeleted: false, HasPaidPremium: true,
            PremiumTrialStartedAtUtc: null, PremiumTrialEndsAtUtc: null, IsEmailConfirmed: true));
        IBillingRecurringProviderGateway gateway = Substitute.For<IBillingRecurringProviderGateway>();
        gateway.Provider.Returns(BillingProviderNames.YooKassa);
        gateway.CreateRecurringPaymentAsync(Arg.Any<BillingRecurringPaymentRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(async call => {
                BillingRecurringPaymentRequestModel request = call.Arg<BillingRecurringPaymentRequestModel>();
                Assert.Equal(first.Id.Value, request.UserId);
                BillingWebhookEventModel webhook = WorkflowWebhook(second, "second_renewed", "second_payment", now) with {
                    ExternalPaymentMethodId = $"pm_{second.Id.Value:N}",
                };
                Assert.True((await WorkflowProcessor(otherProvider, users, Substitute.For<ISender>())
                    .ProcessAsync(BillingProviderNames.YooKassa, "{}", webhook, inboxEvent: null, CancellationToken.None)).IsSuccess);
                return Result.Success(new BillingRecurringPaymentModel("first_payment", request.PaymentMethodId, "price", "monthly", "active",
                    now, now.AddMonths(1), "first_renewed", 299m, "RUB", ProviderMetadataJson: null, OccurredAtUtc: now));
            });
        var renewal = new RenewDueSubscriptionsCommandHandler(subscriptions, provider.GetRequiredService<IBillingPaymentWriteRepository>(),
            users, runner, [gateway], new BillingAccessService(users, subscriptions, TimeProvider.System), TimeProvider.System);

        await renewal.Handle(new RenewDueSubscriptionsCommand(BillingProviderNames.YooKassa, 10), CancellationToken.None);

        await gateway.Received(1).CreateRecurringPaymentAsync(Arg.Any<BillingRecurringPaymentRequestModel>(), Arg.Any<CancellationToken>());
        await using FoodDiaryDbContext verification = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        Assert.Equal("second_renewed", (await verification.BillingSubscriptions.AsNoTracking().SingleAsync(item => item.UserId == second.Id)).LastWebhookEventId);
    }

    private static ISender WorkflowUsers(User user) {
        ISender users = Substitute.For<ISender>();
        users.Send(new GetUserBillingProfileIncludingDeletedQuery(UserId: user.Id), Arg.Any<CancellationToken>()).Returns(new UserBillingProfileModel(
            user.Id, user.Email, IsActive: true, IsDeleted: false, HasPaidPremium: true,
            PremiumTrialStartedAtUtc: null, PremiumTrialEndsAtUtc: null, IsEmailConfirmed: true));
        return users;
    }

    private static BillingWebhookEventModel WorkflowWebhook(User user, string eventId, string paymentId, DateTime now) =>
        new(eventId, "payment.succeeded", user.Id.Value.ToString(), paymentId, "pm_saved", "price", "monthly", "active",
            now, now.AddMonths(1), CancelAtPeriodEnd: false, CanceledAtUtc: null, TrialStartUtc: null, TrialEndUtc: null,
            299m, "RUB", ProviderMetadataJson: null, user.Id.Value, OccurredAtUtc: now, IsAuthoritativeSnapshot: true);

    private static BillingWebhookEventProcessor WorkflowProcessor(ServiceProvider provider, ISender users,
        ISender marketing) {
        IBillingSubscriptionWriteRepository subscriptions = provider.GetRequiredService<IBillingSubscriptionWriteRepository>();
        IBillingPaymentWriteRepository payments = provider.GetRequiredService<IBillingPaymentWriteRepository>();
        return new BillingWebhookEventProcessor(provider.GetRequiredService<IBillingWebhookEventWriteRepository>(),
            provider.GetRequiredService<IBillingTransactionRunner>(), new BillingWebhookContextResolver(subscriptions, users, payments),
            new BillingWebhookSubscriptionWriter(subscriptions, TimeProvider.System), new BillingWebhookPaymentRecorder(payments),
            new BillingWebhookPremiumRoleSyncer(subscriptions, new BillingAccessService(users, subscriptions, TimeProvider.System),
                marketing, TimeProvider.System), TimeProvider.System);
    }
}
