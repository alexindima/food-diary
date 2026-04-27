using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Billing.Commands.CreateCheckoutSession;
using FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;
using FoodDiary.Application.Billing.Services;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Billing;

public sealed class BillingFeatureTests {
    private static readonly DateTime Now = new(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateCheckoutSession_WithRequestedProvider_CreatesPendingSubscriptionAndCheckoutPayment() {
        var user = User.Create("buyer@example.com", "hash");
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository();
        var paymentRepository = new RecordingBillingPaymentRepository();
        var yooKassaGateway = new FakeBillingProviderGateway(
            BillingProviderNames.YooKassa,
            checkoutSession: new BillingCheckoutSessionModel(
                "pay_123",
                "https://checkout.example/pay_123",
                "customer_123",
                "price_monthly",
                "monthly"));
        var accessor = new FakeBillingProviderGatewayAccessor(
            activeProvider: new FakeBillingProviderGateway(BillingProviderNames.Paddle),
            yooKassaGateway);
        var handler = new CreateCheckoutSessionCommandHandler(
            userRepository,
            subscriptionRepository,
            paymentRepository,
            accessor);

        var result = await handler.Handle(
            new CreateCheckoutSessionCommand(user.Id.Value, " Monthly ", BillingProviderNames.YooKassa),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://checkout.example/pay_123", result.Value.Url);
        var subscription = Assert.Single(subscriptionRepository.Subscriptions);
        Assert.Equal(BillingProviderNames.YooKassa, subscription.Provider);
        Assert.Equal("customer_123", subscription.ExternalCustomerId);
        Assert.Equal(BillingSubscription.PendingCheckoutStatus, subscription.Status);

        var payment = Assert.Single(paymentRepository.Payments);
        Assert.Equal(BillingPaymentKinds.Checkout, payment.Kind);
        Assert.Equal("pay_123", payment.ExternalPaymentId);
        Assert.Equal(subscription.Id, payment.BillingSubscriptionId);
    }

    [Fact]
    public async Task ProcessBillingWebhook_ForNewEvent_StoresSubscriptionPaymentWebhookAndAddsPremiumRole() {
        var user = User.Create("premium@example.com", "hash");
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository();
        var paymentRepository = new RecordingBillingPaymentRepository();
        var webhookEventRepository = new RecordingBillingWebhookEventRepository();
        var providerMetadataJson = "{\"payment_id\":\"pay_456\"}";
        var webhookModel = new BillingWebhookEventModel(
            "evt_1",
            "payment.succeeded",
            "customer_456",
            "pay_456",
            "pm_456",
            "price_monthly",
            "monthly",
            "active",
            Now,
            Now.AddMonths(1),
            false,
            null,
            null,
            null,
            7.99m,
            "USD",
            providerMetadataJson,
            user.Id.Value);
        var gateway = new FakeBillingProviderGateway(BillingProviderNames.YooKassa, webhookEvent: webhookModel);
        var handler = CreateWebhookHandler(
            gateway,
            userRepository,
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        var result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{\"event\":\"payment.succeeded\"}", string.Empty),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var subscription = Assert.Single(subscriptionRepository.Subscriptions);
        Assert.Equal("active", subscription.Status);
        Assert.Equal("pay_456", subscription.ExternalSubscriptionId);
        Assert.Equal("pm_456", subscription.ExternalPaymentMethodId);
        Assert.Equal(providerMetadataJson, subscription.ProviderMetadataJson);

        var payment = Assert.Single(paymentRepository.Payments);
        Assert.Equal(BillingPaymentKinds.Webhook, payment.Kind);
        Assert.Equal("pay_456", payment.ExternalPaymentId);
        Assert.Equal(7.99m, payment.Amount);
        Assert.Equal("USD", payment.Currency);
        Assert.Equal("evt_1", payment.WebhookEventId);

        var webhookEvent = Assert.Single(webhookEventRepository.Events);
        Assert.Equal("evt_1", webhookEvent.EventId);
        Assert.Equal("processed", webhookEvent.Status);
        Assert.True(user.HasRole(RoleNames.Premium));
        Assert.Equal(1, userRepository.UpdateCount);
    }

    [Fact]
    public async Task ProcessBillingWebhook_ForDuplicateEvent_ReturnsSuccessWithoutMutation() {
        var user = User.Create("premium@example.com", "hash");
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository();
        var paymentRepository = new RecordingBillingPaymentRepository();
        var webhookEventRepository = new RecordingBillingWebhookEventRepository();
        webhookEventRepository.ProcessedEventIds.Add("evt_duplicate");
        var gateway = new FakeBillingProviderGateway(
            BillingProviderNames.YooKassa,
            webhookEvent: new BillingWebhookEventModel(
                "evt_duplicate",
                "payment.succeeded",
                "customer_456",
                "pay_456",
                "pm_456",
                "price_monthly",
                "monthly",
                "active",
                Now,
                Now.AddMonths(1),
                false,
                null,
                null,
                null,
                7.99m,
                "USD",
                null,
                user.Id.Value));
        var handler = CreateWebhookHandler(
            gateway,
            userRepository,
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        var result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(subscriptionRepository.Subscriptions);
        Assert.Empty(paymentRepository.Payments);
        Assert.Empty(webhookEventRepository.Events);
        Assert.Equal(0, userRepository.UpdateCount);
    }

    [Fact]
    public async Task BillingRenewalService_ForDueSubscription_UpdatesSubscriptionAddsPaymentAndKeepsPremiumRole() {
        var premiumRole = Role.Create(RoleNames.Premium);
        var user = User.Create("premium@example.com", "hash");
        user.ReplaceRoles([premiumRole]);
        var subscription = BillingSubscription.CreatePending(
            user.Id,
            BillingProviderNames.YooKassa,
            "customer_renewal",
            "price_monthly",
            "monthly");
        subscription.ApplyProviderSnapshot(
            BillingProviderNames.YooKassa,
            "pay_initial",
            "pm_renewal",
            "price_monthly",
            "monthly",
            "active",
            Now.AddMonths(-1),
            Now.AddMinutes(-1),
            false,
            null,
            null,
            null,
            "evt_initial",
            Now.AddMonths(-1),
            "{\"initial\":true}");
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository(subscription);
        var paymentRepository = new RecordingBillingPaymentRepository();
        var renewalGateway = new FakeRecurringBillingGateway(
            BillingProviderNames.YooKassa,
            new BillingRecurringPaymentModel(
                "pay_renewed",
                "pm_renewal",
                "price_monthly",
                "monthly",
                "active",
                Now,
                Now.AddMonths(1),
                "evt_renewed",
                7.99m,
                "USD",
                "{\"renewed\":true}"));
        var service = new BillingRenewalService(
            subscriptionRepository,
            paymentRepository,
            userRepository,
            [renewalGateway],
            new BillingAccessService(userRepository, new FixedDateTimeProvider(Now)),
            new FixedDateTimeProvider(Now));

        var result = await service.RenewDueSubscriptionsAsync(BillingProviderNames.YooKassa, 10, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        Assert.Equal(1, result.Renewed);
        Assert.Equal(0, result.Failed);
        Assert.Equal("pay_renewed", subscription.ExternalSubscriptionId);
        Assert.Equal(Now.AddMonths(1), subscription.CurrentPeriodEndUtc);
        Assert.Equal("{\"renewed\":true}", subscription.ProviderMetadataJson);

        var payment = Assert.Single(paymentRepository.Payments);
        Assert.Equal(BillingPaymentKinds.Renewal, payment.Kind);
        Assert.Equal("pay_renewed", payment.ExternalPaymentId);
        Assert.Equal("pm_renewal", payment.ExternalPaymentMethodId);
        Assert.Equal(7.99m, payment.Amount);
        Assert.True(user.HasRole(RoleNames.Premium));
    }

    [Fact]
    public async Task BillingRenewalService_WhenProviderMissing_DoesNotProcessSubscriptions() {
        var service = new BillingRenewalService(
            new InMemoryBillingSubscriptionRepository(),
            new RecordingBillingPaymentRepository(),
            new FakeUserRepository(),
            [],
            new BillingAccessService(new FakeUserRepository(), new FixedDateTimeProvider(Now)),
            new FixedDateTimeProvider(Now));

        var result = await service.RenewDueSubscriptionsAsync(BillingProviderNames.YooKassa, 10, CancellationToken.None);

        Assert.Equal(0, result.Processed);
        Assert.Equal(0, result.Renewed);
        Assert.Equal(0, result.Failed);
    }

    private static ProcessBillingWebhookCommandHandler CreateWebhookHandler(
        IBillingProviderGateway gateway,
        FakeUserRepository userRepository,
        InMemoryBillingSubscriptionRepository subscriptionRepository,
        RecordingBillingPaymentRepository paymentRepository,
        RecordingBillingWebhookEventRepository webhookEventRepository) {
        var dateTimeProvider = new FixedDateTimeProvider(Now);
        return new ProcessBillingWebhookCommandHandler(
            new FakeBillingProviderGatewayAccessor(gateway, gateway),
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository,
            userRepository,
            new BillingAccessService(userRepository, dateTimeProvider),
            dateTimeProvider);
    }

    private sealed class FakeUserRepository(params User[] users) : IUserRepository {
        private readonly List<User> _users = users.ToList();
        private readonly Role _premiumRole = Role.Create(RoleNames.Premium);

        public int UpdateCount { get; private set; }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user => string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) =>
            GetByEmailAsync(email, cancellationToken);

        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user => user.Id == id));

        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) =>
            GetByIdAsync(id, cancellationToken);

        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(
            long telegramUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
            string? search,
            int page,
            int limit,
            bool includeDeleted,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<User> Items, int TotalItems)>((_users, _users.Count));

        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)>
            GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) =>
            Task.FromResult((_users.Count, _users.Count, 0, 0, (IReadOnlyList<User>)_users.Take(recentLimit).ToList()));

        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(
            IReadOnlyList<string> names,
            CancellationToken cancellationToken = default) {
            IReadOnlyList<Role> roles = names.Contains(RoleNames.Premium, StringComparer.Ordinal)
                ? [_premiumRole]
                : [];
            return Task.FromResult(roles);
        }

        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) {
            _users.Add(user);
            return Task.FromResult(user);
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) {
            UpdateCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryBillingSubscriptionRepository(params BillingSubscription[] subscriptions)
        : IBillingSubscriptionRepository {
        public List<BillingSubscription> Subscriptions { get; } = subscriptions.ToList();
        public int UpdateCount { get; private set; }

        public Task<BillingSubscription?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Subscriptions.FirstOrDefault(subscription => subscription.UserId == userId));

        public Task<BillingSubscription?> GetByExternalCustomerIdAsync(
            string provider,
            string externalCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Subscriptions.FirstOrDefault(subscription =>
                string.Equals(subscription.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(subscription.ExternalCustomerId, externalCustomerId, StringComparison.Ordinal)));

        public Task<BillingSubscription?> GetByExternalSubscriptionIdAsync(
            string provider,
            string externalSubscriptionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Subscriptions.FirstOrDefault(subscription =>
                string.Equals(subscription.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(subscription.ExternalSubscriptionId, externalSubscriptionId, StringComparison.Ordinal)));

        public Task<BillingSubscription?> GetByExternalPaymentMethodIdAsync(
            string provider,
            string externalPaymentMethodId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Subscriptions.FirstOrDefault(subscription =>
                string.Equals(subscription.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(subscription.ExternalPaymentMethodId, externalPaymentMethodId, StringComparison.Ordinal)));

        public Task<IReadOnlyList<BillingSubscription>> GetDueForRenewalAsync(
            string provider,
            DateTime dueAtUtc,
            int limit,
            CancellationToken cancellationToken = default) {
            IReadOnlyList<BillingSubscription> dueSubscriptions = Subscriptions
                .Where(subscription =>
                    string.Equals(subscription.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                    subscription.NextBillingAttemptUtc.HasValue &&
                    subscription.NextBillingAttemptUtc <= dueAtUtc)
                .Take(limit)
                .ToList();
            return Task.FromResult(dueSubscriptions);
        }

        public Task<BillingSubscription> AddAsync(
            BillingSubscription subscription,
            CancellationToken cancellationToken = default) {
            Subscriptions.Add(subscription);
            return Task.FromResult(subscription);
        }

        public Task UpdateAsync(BillingSubscription subscription, CancellationToken cancellationToken = default) {
            UpdateCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingBillingPaymentRepository : IBillingPaymentRepository {
        public List<BillingPayment> Payments { get; } = [];

        public Task<BillingPayment?> GetByExternalPaymentIdAsync(
            string provider,
            string externalPaymentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Payments.FirstOrDefault(payment =>
                string.Equals(payment.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(payment.ExternalPaymentId, externalPaymentId, StringComparison.Ordinal)));

        public Task<BillingPayment> AddAsync(BillingPayment payment, CancellationToken cancellationToken = default) {
            Payments.Add(payment);
            return Task.FromResult(payment);
        }
    }

    private sealed class RecordingBillingWebhookEventRepository : IBillingWebhookEventRepository {
        public HashSet<string> ProcessedEventIds { get; } = new(StringComparer.Ordinal);
        public List<BillingWebhookEvent> Events { get; } = [];

        public Task<bool> ExistsAsync(string provider, string eventId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ProcessedEventIds.Contains(eventId));

        public Task<BillingWebhookEvent> AddAsync(
            BillingWebhookEvent webhookEvent,
            CancellationToken cancellationToken = default) {
            ProcessedEventIds.Add(webhookEvent.EventId);
            Events.Add(webhookEvent);
            return Task.FromResult(webhookEvent);
        }
    }

    private sealed class FakeBillingProviderGatewayAccessor(
        IBillingProviderGateway activeProvider,
        params IBillingProviderGateway[] providers)
        : IBillingProviderGatewayAccessor {
        private readonly Dictionary<string, IBillingProviderGateway> _providers = providers
            .Concat([activeProvider])
            .GroupBy(provider => provider.Provider, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        public IBillingProviderGateway GetActiveProvider() => activeProvider;

        public IBillingProviderGateway? GetProviderOrDefault(string provider) =>
            _providers.GetValueOrDefault(provider);
    }

    private sealed class FakeBillingProviderGateway(
        string provider,
        BillingCheckoutSessionModel? checkoutSession = null,
        BillingWebhookEventModel? webhookEvent = null)
        : IBillingProviderGateway {
        public string Provider { get; } = provider;

        public Task<Result<BillingCheckoutSessionModel>> CreateCheckoutSessionAsync(
            BillingCheckoutSessionRequestModel request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(checkoutSession ?? new BillingCheckoutSessionModel(
                "session",
                "https://checkout.example/session",
                request.ExistingCustomerId ?? "customer",
                "price",
                request.Plan)));

        public Task<Result<BillingPortalSessionModel>> CreatePortalSessionAsync(
            BillingPortalSessionRequestModel request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<BillingWebhookEventModel?>> ParseWebhookEventAsync(
            string payload,
            string signatureHeader,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success<BillingWebhookEventModel?>(webhookEvent));
    }

    private sealed class FakeRecurringBillingGateway(
        string provider,
        BillingRecurringPaymentModel renewal)
        : IBillingRecurringProviderGateway {
        public string Provider { get; } = provider;

        public Task<Result<BillingRecurringPaymentModel>> CreateRecurringPaymentAsync(
            BillingRecurringPaymentRequestModel request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(renewal));
    }

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider {
        public DateTime UtcNow { get; } = utcNow;
    }
}
