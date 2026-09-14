using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.RenewDueSubscriptions;
using FoodDiary.Modules.Billing.Application.Services;
using FoodDiary.Modules.Billing.Contracts.Commands.RenewDueSubscriptions;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Results;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Modules.Billing.Contracts.Models;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

public partial class BillingFeatureTests {
    [Theory]
    [InlineData("active")]
    [InlineData("pending")]
    [InlineData("canceled")]
    [InlineData("transport-error")]
    public async Task RenewalResponse_AfterNewerWebhook_DoesNotOverwriteSubscription(string responseStatus) {
        User user = CreatePremiumUser("renewal-race@example.com");
        BillingSubscription subscription = CreateSubscriptionSnapshot(user, BillingProviderNames.YooKassa,
            "customer_race", "pay_initial", "pm_race", "active", Now.AddMonths(-1), Now.AddMinutes(-1), "initial", Now.AddMonths(-1));
        var subscriptions = new InMemoryBillingSubscriptionRepository(subscription);
        var payments = new RecordingBillingPaymentRepository();
        var users = new FakeUserRepository(user);
        IBillingRecurringProviderGateway gateway = Substitute.For<IBillingRecurringProviderGateway>();
        gateway.Provider.Returns(BillingProviderNames.YooKassa);
        gateway.CreateRecurringPaymentAsync(Arg.Any<BillingRecurringPaymentRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(_ => {
                subscription.ApplyProviderSnapshot(BillingProviderNames.YooKassa, "pay_newer", "pm_newer", "price_monthly",
                    "monthly", "active", Now, Now.AddMonths(2), cancelAtPeriodEnd: true, canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null, "webhook_newer", Now.AddSeconds(1), providerMetadataJson: null, Now.AddSeconds(1));
                return string.Equals(responseStatus, "transport-error", StringComparison.Ordinal)
                    ? Result.Failure<BillingRecurringPaymentModel>(BillingErrors.ProviderOperationFailed(BillingProviderNames.YooKassa, "timeout"))
                    : Result.Success(CreateRenewalPayment("pay_old", "pm_race", "old_result") with { Status = responseStatus });
            });
        var transactions = new NoOpBillingTransactionRunner();
        RenewDueSubscriptionsCommandHandler handler = CreateRenewalHandler(subscriptions, payments, users, gateway, transactions);

        await handler.Handle(new RenewDueSubscriptionsCommand(BillingProviderNames.YooKassa, 10), CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal("webhook_newer", subscription.LastWebhookEventId),
            () => Assert.Equal("active", subscription.Status),
            () => Assert.Equal("pay_newer", subscription.ExternalSubscriptionId),
            () => Assert.True(subscription.CancelAtPeriodEnd),
            () => Assert.Equal(Now.AddMonths(2), subscription.CurrentPeriodEndUtc),
            () => Assert.Equal(0, subscriptions.UpdateCount),
            () => Assert.Equal(0, users.RoleMembershipWriteCount),
            () => Assert.Equal($"billing-user:{user.Id.Value:N}", transactions.LastSerializationKey));
        if (string.Equals(responseStatus, "transport-error", StringComparison.Ordinal)) {
            Assert.Empty(payments.Payments);
        } else {
            Assert.Equal(responseStatus, Assert.Single(payments.Payments).Status);
        }
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("past_due")]
    public async Task UnresolvedRenewal_IsPolledAfterBackoffWithoutCreatingAnotherPayment(string initialStatus) {
        User user = CreatePremiumUser("pending-renewal@example.com");
        DateTime periodEnd = Now.AddMinutes(-1);
        var subscriptions = new InMemoryBillingSubscriptionRepository(CreateSubscriptionSnapshot(user, BillingProviderNames.YooKassa,
            "customer_pending", "pay_initial", "pm_pending", "active", Now.AddMonths(-1), periodEnd, "initial", Now.AddMonths(-1)));
        var payments = new RecordingBillingPaymentRepository();
        var users = new FakeUserRepository(user);
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(new DateTimeOffset(Now));
        IBillingRecurringProviderGateway gateway = Substitute.For<IBillingRecurringProviderGateway>();
        gateway.Provider.Returns(BillingProviderNames.YooKassa);
        gateway.CreateRecurringPaymentAsync(Arg.Any<BillingRecurringPaymentRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(CreateRenewalPayment("pay_pending", "pm_pending", "pending") with {
                Status = initialStatus,
                CurrentPeriodStartUtc = null,
                CurrentPeriodEndUtc = periodEnd,
            }));
        gateway.GetRecurringPaymentAsync("pay_pending", Arg.Any<BillingRecurringPaymentRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(CreateRenewalPayment("pay_pending", "pm_pending", "confirmed")));
        var handler = new RenewDueSubscriptionsCommandHandler(subscriptions, payments, users, new NoOpBillingTransactionRunner(),
            [gateway], new BillingAccessService(users, subscriptions, clock), clock);
        var command = new RenewDueSubscriptionsCommand(BillingProviderNames.YooKassa, 10);

        await handler.Handle(command, CancellationToken.None);
        Assert.Equal(Now.AddHours(1), Assert.Single(subscriptions.Subscriptions).NextBillingAttemptUtc);
        Assert.Equal(0, (await handler.Handle(command, CancellationToken.None)).Processed);
        clock.GetUtcNow().Returns(new DateTimeOffset(Now.AddHours(1)));
        BillingRenewalRunResult result = await handler.Handle(command, CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(1, result.Renewed),
            () => Assert.Equal("active", Assert.Single(payments.Payments).Status),
            () => Assert.Equal("confirmed", subscriptions.Subscriptions[0].LastWebhookEventId));
        await gateway.Received(1).CreateRecurringPaymentAsync(Arg.Any<BillingRecurringPaymentRequestModel>(), Arg.Any<CancellationToken>());
        await gateway.Received(1).GetRecurringPaymentAsync("pay_pending", Arg.Is<BillingRecurringPaymentRequestModel>(r => r.CurrentPeriodEndUtc == periodEnd), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeclinedRenewal_OpensNewAttemptAfterBackoff_AndTransportFailureReusesItsKey() {
        User user = CreatePremiumUser("declined-renewal@example.com");
        DateTime periodEnd = Now.AddMinutes(-1);
        var subscriptions = new InMemoryBillingSubscriptionRepository(CreateSubscriptionSnapshot(user, BillingProviderNames.YooKassa,
            "customer_declined", "pay_initial", "pm_declined", "active", Now.AddMonths(-1), periodEnd, "initial", Now.AddMonths(-1)));
        var payments = new RecordingBillingPaymentRepository();
        var users = new FakeUserRepository(user);
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(new DateTimeOffset(Now));
        var keys = new List<string>();
        IBillingRecurringProviderGateway gateway = Substitute.For<IBillingRecurringProviderGateway>();
        gateway.Provider.Returns(BillingProviderNames.YooKassa);
        gateway.CreateRecurringPaymentAsync(Arg.Any<BillingRecurringPaymentRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                keys.Add(call.Arg<BillingRecurringPaymentRequestModel>().IdempotenceKey);
                return keys.Count == 1
                    ? Result.Success(CreateRenewalPayment("pay_declined", "pm_declined", "declined") with {
                        Status = "canceled",
                        CurrentPeriodStartUtc = null,
                        CurrentPeriodEndUtc = periodEnd,
                    })
                    : Result.Failure<BillingRecurringPaymentModel>(BillingErrors.ProviderOperationFailed(BillingProviderNames.YooKassa, "timeout"));
            });
        var handler = new RenewDueSubscriptionsCommandHandler(subscriptions, payments, users, new NoOpBillingTransactionRunner(),
            [gateway], new BillingAccessService(users, subscriptions, clock), clock);
        var command = new RenewDueSubscriptionsCommand(BillingProviderNames.YooKassa, 10);

        await handler.Handle(command, CancellationToken.None);
        Assert.Equal(0, (await handler.Handle(command, CancellationToken.None)).Processed);
        clock.GetUtcNow().Returns(new DateTimeOffset(Now.AddHours(1)));
        await handler.Handle(command, CancellationToken.None);
        clock.GetUtcNow().Returns(new DateTimeOffset(Now.AddHours(2)));
        await handler.Handle(command, CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(3, keys.Count),
            () => Assert.NotEqual(keys[0], keys[1], StringComparer.Ordinal),
            () => Assert.Equal(keys[1], keys[2]),
            () => Assert.Equal("canceled", Assert.Single(payments.Payments).Status),
            () => Assert.Equal(Now.AddHours(3), subscriptions.Subscriptions[0].NextBillingAttemptUtc));
    }
}
