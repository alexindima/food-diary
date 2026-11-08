using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.SendTelegramCancellationConfirmations;
using FoodDiary.Modules.Billing.Contracts.Commands.SendTelegramCancellationConfirmations;
using FoodDiary.Modules.Billing.Contracts.Models;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfileIncludingDeleted;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

public partial class BillingFeatureTests {
    [Fact]
    public async Task CancellationTelegram_SendsConfirmedLocalizedExpiryOnceOutsideTransactions() {
        var fixture = new TelegramCancellationFixture();
        Assert.Equal(new BillingCancellationTelegramRunResult(1, 0, 0), await fixture.RunAsync());
        Assert.Equal(new BillingCancellationTelegramRunResult(0, 1, 0), await fixture.RunAsync());

        string text = Assert.Single(fixture.Delivered);
        Assert.Contains("Автопродление Premium в FoodDiary отключено.", text, StringComparison.Ordinal);
        Assert.Contains("08.12.2026 15:12 (UTC+04:00)", text, StringComparison.Ordinal);
        Assert.Equal(TelegramCancellationFixture.Instant, fixture.HandledAt);
        Assert.Equal(fixture.End, fixture.Subscription.CurrentPeriodEndUtc);
        Assert.True(fixture.Subscription.CancelAtPeriodEnd);
        Assert.Equal("scheduled", fixture.Cancellation.Outcome);
        Assert.All(fixture.Sender.ReceivedCalls(), call => Assert.IsType<GetUserBillingProfileIncludingDeletedQuery>(call.GetArguments()[0]));
    }

    [Theory]
    [InlineData("en", "Asia/Kathmandu", "12/8/2026 4:57 PM (UTC+05:45)")]
    [InlineData("en-US", "America/New_York", "12/8/2026 6:12 AM (UTC-05:00)")]
    [InlineData(null, "invalid-zone", "12/8/2026 11:12 AM (UTC+00:00)")]
    [InlineData("de", null, "12/8/2026 11:12 AM (UTC+00:00)")]
    [InlineData("RU-ru", "Asia/Tbilisi", "08.12.2026 15:12 (UTC+04:00)")]
    public async Task CancellationTelegram_UsesProfileLocaleAndExpiryOffset(string? language, string? zone, string expected) {
        var fixture = new TelegramCancellationFixture();
        fixture.Profile = fixture.Profile with { Language = language, TimeZoneId = zone };
        await fixture.RunAsync();
        Assert.Contains(expected, NormalizeDateSpaces(Assert.Single(fixture.Delivered)), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, "3/28/2027 1:30 AM (UTC+01:00)")]
    [InlineData(1, "3/28/2027 3:30 AM (UTC+02:00)")]
    public async Task CancellationTelegram_UsesOffsetAtExpiryRatherThanAtCancellation(int hour, string expected) {
        var fixture = new TelegramCancellationFixture();
        fixture.End = new DateTime(2027, 3, 28, hour, 30, 0, DateTimeKind.Utc);
        fixture.Profile = fixture.Profile with { Language = "en", TimeZoneId = "Europe/Berlin" };
        await fixture.RunAsync();
        Assert.Contains(expected, NormalizeDateSpaces(Assert.Single(fixture.Delivered)), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("deleted")]
    [InlineData("unlinked")]
    [InlineData("changed-payer")]
    [InlineData("foreign-profile")]
    [InlineData("missing-profile")]
    [InlineData("missing-subscription")]
    [InlineData("foreign-subscription-owner")]
    [InlineData("changed-subscription")]
    [InlineData("changed-provider")]
    [InlineData("resumed")]
    [InlineData("removed-record")]
    [InlineData("expired")]
    [InlineData("changed-period")]
    [InlineData("no-paid-access")]
    [InlineData("canceled")]
    public async Task CancellationTelegram_IneligibleCurrentStateIsHandledWithoutSending(string scenario) {
        var fixture = new TelegramCancellationFixture();
        switch (scenario) {
            case "inactive": fixture.Profile = fixture.Profile with { IsActive = false }; break;
            case "deleted": fixture.Profile = fixture.Profile with { IsDeleted = true }; break;
            case "unlinked": fixture.Profile = fixture.Profile with { TelegramUserId = null }; break;
            case "changed-payer": fixture.Profile = fixture.Profile with { TelegramUserId = 987 }; break;
            case "foreign-profile": fixture.Profile = fixture.Profile with { UserId = UserId.New() }; break;
            case "missing-profile": fixture.ProfileMissing = true; break;
            case "missing-subscription": fixture.SubscriptionMissing = true; break;
            case "foreign-subscription-owner": fixture.Subscription = fixture.Subscription with { UserId = Guid.NewGuid() }; break;
            case "changed-subscription": fixture.Subscription = fixture.Subscription with { ExternalSubscriptionId = "new-subscription" }; break;
            case "changed-provider": fixture.Subscription = fixture.Subscription with { Provider = BillingProviderNames.Paddle }; break;
            case "resumed": fixture.Subscription = fixture.Subscription with { CancelAtPeriodEnd = false }; break;
            case "removed-record": fixture.Cancellation = fixture.Cancellation with { Outcome = "cancellation_removed" }; break;
            case "expired": fixture.End = TelegramCancellationFixture.Instant; break;
            case "changed-period": fixture.Subscription = fixture.Subscription with { CurrentPeriodEndUtc = fixture.End.AddDays(30) }; break;
            case "no-paid-access": fixture.Profile = fixture.Profile with { HasPaidPremium = false }; break;
            case "canceled": fixture.Subscription = fixture.Subscription with { Status = "canceled" }; break;
        }
        Assert.Equal(new BillingCancellationTelegramRunResult(0, 1, 0), await fixture.RunAsync());
        Assert.Empty(fixture.Delivered);
        Assert.NotNull(fixture.HandledAt);
    }

    [Fact]
    public async Task CancellationTelegram_TransientFailureKeepsIntentForRetry() {
        var fixture = new TelegramCancellationFixture { Delivery = TelegramMessageDeliveryResult.Retry };
        Assert.Equal(new BillingCancellationTelegramRunResult(0, 0, 1), await fixture.RunAsync());
        Assert.Null(fixture.HandledAt);
        fixture.Delivery = TelegramMessageDeliveryResult.Sent;
        Assert.Equal(new BillingCancellationTelegramRunResult(1, 0, 0), await fixture.RunAsync());
        Assert.NotNull(fixture.HandledAt);
    }

    [Fact]
    public async Task CancellationTelegram_BlockedRecipientIsHandledWithoutRepeatedDelivery() {
        var fixture = new TelegramCancellationFixture { Delivery = TelegramMessageDeliveryResult.RecipientUnavailable };
        Assert.Equal(new BillingCancellationTelegramRunResult(0, 1, 0), await fixture.RunAsync());
        Assert.Equal(new BillingCancellationTelegramRunResult(0, 1, 0), await fixture.RunAsync());
        Assert.Single(fixture.Delivered);
    }

    [Fact]
    public async Task CancellationTelegram_TransactionReplayDoesNotRepeatProviderCall() {
        var fixture = new TelegramCancellationFixture();
        fixture.Transactions.Attempts = 2;
        Assert.Equal(new BillingCancellationTelegramRunResult(1, 0, 0), await fixture.RunAsync());
        Assert.Single(fixture.Delivered);
    }

    [Fact]
    public async Task CancellationTelegram_ConcurrentWorkersRecheckAfterUserLease() {
        var fixture = new TelegramCancellationFixture();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.SendOverride = async _ => {
            entered.TrySetResult();
            await release.Task;
            return TelegramMessageDeliveryResult.Sent;
        };
        Task<BillingCancellationTelegramRunResult> first = fixture.RunAsync();
        await entered.Task;
        Task<BillingCancellationTelegramRunResult> second = fixture.RunAsync();
        release.SetResult();
        BillingCancellationTelegramRunResult[] results = await Task.WhenAll(first, second);
        Assert.Equal(1, results.Sum(item => item.Sent));
        await fixture.Messages.Received(1).SendAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancellationTelegram_CallerCancellationPropagatesAndKeepsIntent() {
        var fixture = new TelegramCancellationFixture();
        using var cancellation = new CancellationTokenSource();
        fixture.SendOverride = async _ => {
            await cancellation.CancelAsync();
            cancellation.Token.ThrowIfCancellationRequested();
            return TelegramMessageDeliveryResult.Sent;
        };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.RunAsync(cancellation.Token));
        Assert.Null(fixture.HandledAt);
    }

    // ICU uses a narrow no-break space before AM/PM; Windows NLS uses a regular space.
    private static string NormalizeDateSpaces(string value) => value.Replace('\u202f', ' ').Replace('\u00a0', ' ');

    private sealed class TelegramCancellationFixture {
        public static readonly DateTime Instant = new(2026, 11, 8, 11, 12, 45, DateTimeKind.Utc);
        public IBillingCancellationRepository Repository { get; } = Substitute.For<IBillingCancellationRepository>();
        public IBillingTelegramMessageSender Messages { get; } = Substitute.For<IBillingTelegramMessageSender>();
        public ISender Sender { get; } = Substitute.For<ISender>();
        public NoticeTransactions Transactions { get; } = new();
        public List<string> Delivered { get; } = [];
        public DateTime? HandledAt { get; private set; }
        public UserBillingProfileModel Profile { get; set; }
        public BillingSubscriptionOverviewReadModel Subscription { get; set; }
        public BillingCancellationTelegramSnapshotModel Cancellation { get; set; }
        public bool ProfileMissing { get; set; }
        public bool SubscriptionMissing { get; set; }
        public TelegramMessageDeliveryResult Delivery { get; set; } = TelegramMessageDeliveryResult.Sent;
        public Func<CancellationToken, Task<TelegramMessageDeliveryResult>>? SendOverride { get; set; }
        public DateTime End {
            get => Cancellation.EffectiveAtUtc;
            set {
                Cancellation = Cancellation with { EffectiveAtUtc = value };
                Subscription = Subscription with { CurrentPeriodEndUtc = value };
            }
        }
        private readonly SendTelegramCancellationConfirmationsCommandHandler _handler;

        public TelegramCancellationFixture() {
            var owner = UserId.New();
            var candidate = new BillingCancellationTelegramCandidateModel(BillingCancellationId.New(), owner);
            var end = new DateTime(2026, 12, 8, 11, 12, 45, DateTimeKind.Utc);
            Profile = new UserBillingProfileModel(owner, Email: null, IsActive: true, IsDeleted: false, HasPaidPremium: true,
                PremiumTrialStartedAtUtc: null, PremiumTrialEndsAtUtc: null, Language: "ru", TimeZoneId: "Asia/Tbilisi", TelegramUserId: 123);
            Subscription = new BillingSubscriptionOverviewReadModel(Guid.NewGuid(), owner.Value, BillingProviderNames.TelegramStars,
                "123:" + owner.Value.ToString("N"), "monthly", "active", Instant, end, CancelAtPeriodEnd: true,
                NextBillingAttemptUtc: null, ExternalSubscriptionId: "owned-subscription");
            Cancellation = new BillingCancellationTelegramSnapshotModel(BillingProviderNames.TelegramStars, "owned-subscription", "scheduled", end, HandledAtUtc: null);
            Repository.GetPendingTelegramConfirmationsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([candidate]);
            Repository.GetTelegramConfirmationAsync(candidate.Id, owner, Arg.Any<CancellationToken>()).Returns(_ => Cancellation with { HandledAtUtc = HandledAt });
            Repository.MarkTelegramConfirmationHandledAsync(candidate.Id, owner, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(call => {
                HandledAt ??= call.Arg<DateTime>();
                return Task.CompletedTask;
            });
            IBillingSubscriptionReadModelRepository subscriptions = Substitute.For<IBillingSubscriptionReadModelRepository>();
            subscriptions.GetOverviewReadModelByUserIdAsync(owner, Arg.Any<CancellationToken>()).Returns(_ => SubscriptionMissing ? null : Subscription);
            Sender.Send(Arg.Any<GetUserBillingProfileIncludingDeletedQuery>(), Arg.Any<CancellationToken>()).Returns(_ => ProfileMissing ? null : Profile);
            Messages.SendAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(async call => {
                Assert.Equal(0, Transactions.Depth);
                Assert.Equal(Profile.TelegramUserId, call.Arg<long>());
                Delivered.Add(call.Arg<string>());
                return SendOverride is null ? Delivery : await SendOverride(call.Arg<CancellationToken>());
            });
            _handler = new SendTelegramCancellationConfirmationsCommandHandler(Repository, subscriptions, new NoticeCheckoutLock(), Transactions,
                Messages, Sender, new FixedDateTimeProvider(Instant), NullLogger<SendTelegramCancellationConfirmationsCommandHandler>.Instance);
        }

        public Task<BillingCancellationTelegramRunResult> RunAsync(CancellationToken token = default) =>
            _handler.Handle(new SendTelegramCancellationConfirmationsCommand(100), token);
    }

    private sealed class NoticeTransactions : IBillingTransactionRunner {
        public int Depth { get; private set; }
        public int Attempts { get; set; } = 1;
        public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) =>
            ExecuteSerializedAsync("unused", operation, cancellationToken);
        public async Task ExecuteSerializedAsync(string serializationKey, Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) {
            for (int attempt = 0; attempt < Attempts; attempt++) {
                Depth++;
                try { await operation(cancellationToken); } finally { Depth--; }
            }
        }
    }

    private sealed class NoticeCheckoutLock : IBillingCheckoutLock {
        private readonly SemaphoreSlim _gate = new(1);
        public async Task<IAsyncDisposable> AcquireAsync(Guid userId, CancellationToken cancellationToken = default) {
            await _gate.WaitAsync(cancellationToken);
            return new Lease(_gate);
        }
        private sealed class Lease(SemaphoreSlim gate) : IAsyncDisposable {
            public ValueTask DisposeAsync() { gate.Release(); return ValueTask.CompletedTask; }
        }
    }
}
