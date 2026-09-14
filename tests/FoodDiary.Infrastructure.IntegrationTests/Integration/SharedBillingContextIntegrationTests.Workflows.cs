using System.Text.Json;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;
using FoodDiary.Modules.Billing.Application.Commands.ProcessQueuedBillingWebhook;
using FoodDiary.Modules.Billing.Application.Commands.RenewDueSubscriptions;
using FoodDiary.Modules.Billing.Application.Services;
using FoodDiary.Modules.Billing.Contracts.Commands.RenewDueSubscriptions;
using FoodDiary.Modules.Billing.Contracts.Common;
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
        IUserBillingService users = WorkflowUsers(user);
        IBillingMarketingConversionRecorder marketing = Substitute.For<IBillingMarketingConversionRecorder>();
        int attempts = 0;
        marketing.RecordPremiumStartedAsync(user.Id.Value, Arg.Any<CancellationToken>()).Returns(_ => {
            if (++attempts == 1) {
                throw new InvalidOperationException("Sensitive provider failure after subscription mutation");
            }
            return Task.CompletedTask;
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
        IUserBillingService users = WorkflowUsers(user);
        IBillingRecurringProviderGateway gateway = Substitute.For<IBillingRecurringProviderGateway>();
        gateway.Provider.Returns(BillingProviderNames.YooKassa);
        gateway.CreateRecurringPaymentAsync(Arg.Any<BillingRecurringPaymentRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(async _ => {
                Result webhookResult = await WorkflowProcessor(otherProvider, users, Substitute.For<IBillingMarketingConversionRecorder>())
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

    private static IUserBillingService WorkflowUsers(User user) {
        IUserBillingService users = Substitute.For<IUserBillingService>();
        users.GetProfileIncludingDeletedAsync(user.Id, Arg.Any<CancellationToken>()).Returns(new UserBillingProfileModel(
            user.Id, user.Email, IsActive: true, IsDeleted: false, HasPaidPremium: true,
            PremiumTrialStartedAtUtc: null, PremiumTrialEndsAtUtc: null, IsEmailConfirmed: true));
        return users;
    }

    private static BillingWebhookEventModel WorkflowWebhook(User user, string eventId, string paymentId, DateTime now) =>
        new(eventId, "payment.succeeded", user.Id.Value.ToString(), paymentId, "pm_saved", "price", "monthly", "active",
            now, now.AddMonths(1), CancelAtPeriodEnd: false, CanceledAtUtc: null, TrialStartUtc: null, TrialEndUtc: null,
            299m, "RUB", ProviderMetadataJson: null, user.Id.Value, OccurredAtUtc: now, IsAuthoritativeSnapshot: true);

    private static BillingWebhookEventProcessor WorkflowProcessor(ServiceProvider provider, IUserBillingService users,
        IBillingMarketingConversionRecorder marketing) {
        IBillingSubscriptionWriteRepository subscriptions = provider.GetRequiredService<IBillingSubscriptionWriteRepository>();
        IBillingPaymentWriteRepository payments = provider.GetRequiredService<IBillingPaymentWriteRepository>();
        return new BillingWebhookEventProcessor(provider.GetRequiredService<IBillingWebhookEventWriteRepository>(),
            provider.GetRequiredService<IBillingTransactionRunner>(), new BillingWebhookContextResolver(subscriptions, users, payments),
            new BillingWebhookSubscriptionWriter(subscriptions, TimeProvider.System), new BillingWebhookPaymentRecorder(payments),
            new BillingWebhookPremiumRoleSyncer(subscriptions, new BillingAccessService(users, subscriptions, TimeProvider.System),
                marketing, TimeProvider.System), TimeProvider.System);
    }
}
