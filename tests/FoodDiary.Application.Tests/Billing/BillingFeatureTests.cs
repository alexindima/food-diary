using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;
using FoodDiary.Application.Billing.Queries.GetBillingOverview;
using FoodDiary.Application.Billing.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Billing.Models;
using System.Reflection;

namespace FoodDiary.Application.Tests.Billing;

[ExcludeFromCodeCoverage]
public partial class BillingFeatureTests {
    private static readonly DateTime Now = new(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

    private static GetBillingOverviewQueryHandler CreateBillingOverviewHandler(
        IBillingUserContextService billingUserContextService,
        IBillingSubscriptionReadModelRepository billingSubscriptionRepository,
        IBillingPublicConfigProvider billingPublicConfigProvider,
        TimeProvider dateTimeProvider) =>
        new(new BillingOverviewReadService(
            billingUserContextService,
            billingSubscriptionRepository,
            billingPublicConfigProvider,
            dateTimeProvider),
            billingUserContextService);

    [Fact]
    public async Task BillingWebhookContextResolver_WithoutSubscriptionOrUserId_ReturnsValidationFailure() {
        var resolver = new BillingWebhookContextResolver(
            new InMemoryBillingSubscriptionRepository(),
            new FakeUserRepository());
        var webhookModel = new BillingWebhookEventModel(
            "evt_missing_user",
            "payment.succeeded",
            "customer_missing",
            ExternalSubscriptionId: null,
            ExternalPaymentMethodId: null,
            "price_monthly",
            "monthly",
            "active",
            Now,
            Now.AddMonths(1),
            CancelAtPeriodEnd: false,
            CanceledAtUtc: null,
            TrialStartUtc: null,
            TrialEndUtc: null,
            Amount: null,
            Currency: null,
            ProviderMetadataJson: null,
            UserId: null);

        Result<BillingWebhookProcessingContext?> result = await resolver.ResolveAsync(
            BillingProviderNames.Paddle,
            webhookModel,
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Billing.WebhookValidationFailed", result.Error.Code);
    }

    [Fact]
    public async Task BillingWebhookContextResolver_WithEmptyWebhookUserId_ReturnsValidationFailure() {
        var resolver = new BillingWebhookContextResolver(
            new InMemoryBillingSubscriptionRepository(),
            new FakeUserRepository());
        var webhookModel = new BillingWebhookEventModel(
            "evt_empty_user",
            "payment.succeeded",
            "customer_empty_user",
            ExternalSubscriptionId: null,
            ExternalPaymentMethodId: null,
            "price_monthly",
            "monthly",
            "active",
            Now,
            Now.AddMonths(1),
            CancelAtPeriodEnd: false,
            CanceledAtUtc: null,
            TrialStartUtc: null,
            TrialEndUtc: null,
            Amount: null,
            Currency: null,
            ProviderMetadataJson: null,
            UserId: Guid.Empty);

        Result<BillingWebhookProcessingContext?> result = await resolver.ResolveAsync(
            BillingProviderNames.YooKassa,
            webhookModel,
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Billing.WebhookValidationFailed", result.Error.Code);
    }

    private static ProcessBillingWebhookCommandHandler CreateWebhookHandler(
        IBillingProviderGateway gateway,
        FakeUserRepository userRepository,
        InMemoryBillingSubscriptionRepository subscriptionRepository,
        RecordingBillingPaymentRepository paymentRepository,
        RecordingBillingWebhookEventRepository webhookEventRepository,
        IBillingMarketingConversionRecorder? marketingConversionRecorder = null) {
        var dateTimeProvider = new FixedDateTimeProvider(Now);
        var billingAccessService = new BillingAccessService(userRepository, subscriptionRepository, dateTimeProvider);
        return new ProcessBillingWebhookCommandHandler(
            new FakeBillingProviderGatewayAccessor(gateway, gateway),
            webhookEventRepository,
            new NoOpBillingTransactionRunner(),
            new BillingWebhookContextResolver(subscriptionRepository, userRepository),
            new BillingWebhookSubscriptionWriter(subscriptionRepository, dateTimeProvider),
            new BillingWebhookPaymentRecorder(paymentRepository),
            new BillingWebhookPremiumRoleSyncer(
                subscriptionRepository,
                userRepository,
                billingAccessService,
                marketingConversionRecorder ?? new NoOpMarketingConversionRecorder(),
                dateTimeProvider));
    }

    private static User CreatePremiumUser(string email) {
        var user = User.Create(email, "hash");
        user.ReplaceRoles([Role.Create(RoleNames.Premium)]);
        return user;
    }

    private static BillingSubscription CreateSubscriptionSnapshot(
        User user,
        string provider,
        string externalCustomerId,
        string? externalSubscriptionId,
        string? externalPaymentMethodId,
        string status,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        string eventId,
        DateTime eventCreatedAtUtc,
        string? metadataJson = null) {
        var subscription = BillingSubscription.CreatePending(
            user.Id,
            provider,
            externalCustomerId,
            "price_monthly",
            "monthly");
        subscription.ApplyProviderSnapshot(
            provider,
            externalSubscriptionId,
            externalPaymentMethodId,
            "price_monthly",
            "monthly",
            status,
            periodStartUtc,
            periodEndUtc,
            cancelAtPeriodEnd: false,
            canceledAtUtc: null,
            trialStartUtc: null,
            trialEndUtc: null,
            eventId,
            eventCreatedAtUtc,
            metadataJson,
            eventCreatedAtUtc);
        return subscription;
    }

    private static BillingRecurringPaymentModel CreateRenewalPayment(
        string externalPaymentId,
        string externalPaymentMethodId,
        string eventId,
        string? metadataJson = null) =>
        new(
            externalPaymentId,
            externalPaymentMethodId,
            "price_monthly",
            "monthly",
            "active",
            Now,
            Now.AddMonths(1),
            eventId,
            7.99m,
            "USD",
            metadataJson);

    private static BillingWebhookEventModel CreateWebhookPaymentEvent(User user, string eventId, string externalSubscriptionId) =>
        new(
            eventId,
            "payment.succeeded",
            "customer_existing_webhook_payment",
            externalSubscriptionId,
            "pm_existing_webhook_payment",
            "price_monthly",
            "monthly",
            "active",
            Now,
            Now.AddMonths(1),
            CancelAtPeriodEnd: false,
            CanceledAtUtc: null,
            TrialStartUtc: null,
            TrialEndUtc: null,
            7.99m,
            "USD",
            ProviderMetadataJson: null,
            user.Id.Value);

    private static BillingRenewalService CreateRenewalService(
        InMemoryBillingSubscriptionRepository subscriptionRepository,
        RecordingBillingPaymentRepository paymentRepository,
        FakeUserRepository userRepository,
        IBillingRecurringProviderGateway renewalGateway) =>
        new(
            subscriptionRepository,
            paymentRepository,
            userRepository,
            new NoOpBillingTransactionRunner(),
            [renewalGateway],
            new BillingAccessService(userRepository, subscriptionRepository, new FixedDateTimeProvider(Now)),
            new FixedDateTimeProvider(Now));

    private static void SetPrivateProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value) {
        PropertyInfo? property = typeof(TTarget).GetProperty(propertyName);
        Assert.NotNull(property);
        property!.SetValue(target, value);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakeUserRepository(params User[] users) : IUserRepository, IUserContextService, IBillingUserContextService, IBillingUserLookupService {
        private readonly List<User> _users = [.. users];
        private readonly Role _premiumRole = Role.Create(RoleNames.Premium);

        public int UpdateCount { get; private set; }
        public int RoleMembershipWriteCount { get; private set; }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user =>
                IsAccessible(user) &&
                string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user =>
                string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user => IsAccessible(user) && user.Id == id));

        public Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) {
            User? user = _users.FirstOrDefault(candidate => IsAccessible(candidate) && candidate.Id == userId);
            return Task.FromResult(user is null
                ? Result.Failure<User>(Errors.Authentication.InvalidToken)
                : Result.Success(user));
        }

        public async Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
            Result<User> result = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
            return result.IsFailure ? result.Error : null;
        }

        public Task<Result<BillingUserProfileModel>> GetAccessibleUserProfileAsync(
            UserId userId,
            CancellationToken cancellationToken) {
            User? user = _users.FirstOrDefault(candidate => IsAccessible(candidate) && candidate.Id == userId);
            return Task.FromResult(user is null
                ? Result.Failure<BillingUserProfileModel>(Errors.Authentication.InvalidToken)
                : Result.Success(new BillingUserProfileModel(
                    user.HasRole(RoleNames.Premium),
                    user.PremiumTrialStartedAtUtc,
                    user.PremiumTrialEndsAtUtc)));
        }

        public Task<User?> GetUserIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken) =>
            GetByIdIncludingDeletedAsync(userId, cancellationToken);

        public Task<bool> CanAccessUserAsync(User user, CancellationToken cancellationToken) =>
            Task.FromResult(IsAccessible(user));

        public Task EnsurePremiumRoleAsync(User user, CancellationToken cancellationToken) {
            RoleMembershipWriteCount++;
            if (!user.HasRole(RoleNames.Premium)) {
                user.ReplaceRoles([.. user.UserRoles.Select(userRole => userRole.Role), _premiumRole]);
            }

            return Task.CompletedTask;
        }

        public Task RemovePremiumRoleAsync(User user, CancellationToken cancellationToken) {
            RoleMembershipWriteCount++;
            if (user.HasRole(RoleNames.Premium)) {
                user.ReplaceRoles([
                    .. user.UserRoles
                        .Select(userRole => userRole.Role)
                        .Where(role => !string.Equals(role.Name, RoleNames.Premium, StringComparison.Ordinal)),
                ]);
            }

            return Task.CompletedTask;
        }

        public Task UpdateUserAsync(User user, CancellationToken cancellationToken) =>
            UpdateAsync(user, cancellationToken);

        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user => user.Id == id));

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
            Task.FromResult((_users.Count, _users.Count, 0, 0, (IReadOnlyList<User>)[.. _users.Take(recentLimit)]));

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

        private static bool IsAccessible(User user) => user is { IsActive: true, DeletedAt: null };
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingUserRoleMembershipService : IUserRoleMembershipService {
        public List<UserId> EnsureRoleUserIds { get; } = [];
        public List<UserId> RemoveRoleUserIds { get; } = [];

        public Task EnsureRoleAsync(UserId userId, string roleName, CancellationToken cancellationToken = default) {
            Assert.Equal(RoleNames.Premium, roleName);
            EnsureRoleUserIds.Add(userId);
            return Task.CompletedTask;
        }

        public Task RemoveRoleAsync(UserId userId, string roleName, CancellationToken cancellationToken = default) {
            Assert.Equal(RoleNames.Premium, roleName);
            RemoveRoleUserIds.Add(userId);
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryBillingSubscriptionRepository(params BillingSubscription[] subscriptions)
        : IBillingSubscriptionRepository {
        public List<BillingSubscription> Subscriptions { get; } = [.. subscriptions];
        public int UpdateCount { get; private set; }

        public Task<BillingSubscription?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Subscriptions.FirstOrDefault(subscription => subscription.UserId == userId));

        public Task<BillingSubscriptionOverviewReadModel?> GetOverviewReadModelByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default) {
            BillingSubscription? subscription = Subscriptions.FirstOrDefault(subscription => subscription.UserId == userId);
            return Task.FromResult(subscription is null
                ? null
                : new BillingSubscriptionOverviewReadModel(
                    subscription.Id,
                    subscription.UserId.Value,
                    subscription.Provider,
                    subscription.ExternalCustomerId,
                    subscription.Plan,
                    subscription.Status,
                    subscription.CurrentPeriodStartUtc,
                    subscription.CurrentPeriodEndUtc,
                    subscription.CancelAtPeriodEnd,
                    subscription.NextBillingAttemptUtc));
        }

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

    [ExcludeFromCodeCoverage]
    private sealed class RecordingBillingPaymentRepository : IBillingPaymentRepository {
        public List<BillingPayment> Payments { get; } = [];
        public bool ThrowAlreadyExistsOnAdd { get; init; }

        public Task<BillingPayment?> GetByExternalPaymentIdAsync(
            string provider,
            string externalPaymentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Payments.FirstOrDefault(payment =>
                string.Equals(payment.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(payment.ExternalPaymentId, externalPaymentId, StringComparison.Ordinal)));

        public Task<BillingPayment> AddAsync(BillingPayment payment, CancellationToken cancellationToken = default) {
            if (ThrowAlreadyExistsOnAdd) {
                throw new BillingPaymentAlreadyExistsException(payment.Provider, payment.ExternalPaymentId);
            }

            Payments.Add(payment);
            return Task.FromResult(payment);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingBillingWebhookEventRepository : IBillingWebhookEventRepository {
        public HashSet<string> ProcessedEventIds { get; } = new(StringComparer.Ordinal);
        public List<BillingWebhookEvent> Events { get; } = [];
        public bool ThrowAlreadyProcessedOnAdd { get; init; }

        public Task<bool> ExistsAsync(string provider, string eventId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ProcessedEventIds.Contains(eventId));

        public Task<BillingWebhookEvent> AddAsync(
            BillingWebhookEvent webhookEvent,
            CancellationToken cancellationToken = default) {
            if (ThrowAlreadyProcessedOnAdd) {
                throw new BillingWebhookEventAlreadyProcessedException(webhookEvent.Provider, webhookEvent.EventId);
            }

            ProcessedEventIds.Add(webhookEvent.EventId);
            Events.Add(webhookEvent);
            return Task.FromResult(webhookEvent);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakeBillingPublicConfigProvider : IBillingPublicConfigProvider {
        public BillingPublicConfigModel GetPublicConfig() =>
            new(BillingProviderNames.Paddle, "test_client_token", [BillingProviderNames.Paddle, BillingProviderNames.YooKassa]);
    }

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
    private sealed class FakeBillingProviderGateway(
        string provider,
        BillingCheckoutSessionModel? checkoutSession = null,
        BillingWebhookEventModel? webhookEvent = null,
        BillingPortalSessionModel? portalSession = null,
        Error? checkoutError = null,
        Error? portalError = null,
        Error? webhookError = null)
        : IBillingProviderGateway {
        public string Provider { get; } = provider;

        public Task<Result<BillingCheckoutSessionModel>> CreateCheckoutSessionAsync(
            BillingCheckoutSessionRequestModel request,
            CancellationToken cancellationToken = default) {
            if (checkoutError is not null) {
                return Task.FromResult(Result.Failure<BillingCheckoutSessionModel>(checkoutError));
            }

            return Task.FromResult(Result.Success(checkoutSession ?? new BillingCheckoutSessionModel(
                "session",
                "https://checkout.example/session",
                request.ExistingCustomerId ?? "customer",
                "price",
                request.Plan)));
        }

        public Task<Result<BillingPortalSessionModel>> CreatePortalSessionAsync(
            BillingPortalSessionRequestModel request,
            CancellationToken cancellationToken = default) {
            if (portalError is not null) {
                return Task.FromResult(Result.Failure<BillingPortalSessionModel>(portalError));
            }

            return Task.FromResult(Result.Success(portalSession ?? new BillingPortalSessionModel(
                $"https://billing.example/{request.CustomerId}")));
        }

        public Task<Result<BillingWebhookEventModel?>> ParseWebhookEventAsync(
            string payload,
            string signatureHeader,
            CancellationToken cancellationToken = default) {
            if (webhookError is not null) {
                return Task.FromResult(Result.Failure<BillingWebhookEventModel?>(webhookError));
            }

            return Task.FromResult(Result.Success<BillingWebhookEventModel?>(webhookEvent));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakeRecurringBillingGateway(
        string provider,
        BillingRecurringPaymentModel renewal)
        : IBillingRecurringProviderGateway {
        public string Provider { get; } = provider;
        public int CreatePaymentCallCount { get; private set; }

        public Task<Result<BillingRecurringPaymentModel>> CreateRecurringPaymentAsync(
            BillingRecurringPaymentRequestModel request,
            CancellationToken cancellationToken = default) {
            CreatePaymentCallCount++;
            return Task.FromResult(Result.Success(renewal));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FailingRecurringBillingGateway(string providerName) : IBillingRecurringProviderGateway {
        public string Provider { get; } = providerName;
        public int CreatePaymentCallCount { get; private set; }

        public Task<Result<BillingRecurringPaymentModel>> CreateRecurringPaymentAsync(
            BillingRecurringPaymentRequestModel request,
            CancellationToken cancellationToken = default) {
            CreatePaymentCallCount++;
            return Task.FromResult(Result.Failure<BillingRecurringPaymentModel>(
                Errors.Billing.ProviderOperationFailed(Provider, "declined")));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoOpBillingTransactionRunner : IBillingTransactionRunner {
        public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoOpMarketingConversionRecorder : IBillingMarketingConversionRecorder {
        public Task RecordPremiumStartedAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingMarketingConversionRecorder : IBillingMarketingConversionRecorder {
        public List<Guid> PremiumStartedUserIds { get; } = [];

        public Task RecordPremiumStartedAsync(Guid userId, CancellationToken cancellationToken = default) {
            PremiumStartedUserIds.Add(userId);
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryMarketingAttributionEventRepository(params MarketingAttributionEventRecord[] seedRecords)
        : IMarketingAttributionEventRepository {
        public List<MarketingAttributionEventRecord> Records { get; } = [.. seedRecords];

        public Task AddAsync(MarketingAttributionEventRecord record, CancellationToken cancellationToken = default) {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task<int> DeleteOlderThanAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<MarketingAttributionEventRecord>> GetSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default) {
            IReadOnlyList<MarketingAttributionEventRecord> matchingRecords = [
                .. Records
                .Where(record => record.OccurredAtUtc >= sinceUtc)
                .OrderByDescending(record => record.OccurredAtUtc),
            ];
            return Task.FromResult(matchingRecords);
        }

        public Task<MarketingAttributionEventRecord?> GetLatestForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Records
                .Where(record => record.UserId == userId)
                .OrderByDescending(record => record.OccurredAtUtc)
                .FirstOrDefault());

        public Task<bool> ExistsForUserAsync(Guid userId, string eventType, CancellationToken cancellationToken = default) =>
            Task.FromResult(Records.Any(record =>
                record.UserId == userId &&
                string.Equals(record.EventType, eventType, StringComparison.Ordinal)));
    }
}
