using System.Globalization;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Common;
using FoodDiary.Modules.Billing.Contracts.Commands.SendTelegramCancellationConfirmations;
using FoodDiary.Modules.Billing.Contracts.Models;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfileIncludingDeleted;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Modules.Billing.Application.Commands.SendTelegramCancellationConfirmations;

public sealed class SendTelegramCancellationConfirmationsCommandHandler(
    IBillingCancellationRepository cancellations,
    IBillingSubscriptionReadModelRepository subscriptions,
    IBillingCheckoutLock checkoutLock,
    IBillingTransactionRunner transactions,
    IBillingTelegramMessageSender messages,
    ISender sender,
    TimeProvider timeProvider,
    ILogger<SendTelegramCancellationConfirmationsCommandHandler> logger)
    : IRequestHandler<SendTelegramCancellationConfirmationsCommand, BillingCancellationTelegramRunResult> {
    public async Task<BillingCancellationTelegramRunResult> Handle(SendTelegramCancellationConfirmationsCommand request, CancellationToken cancellationToken) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.BatchSize, nameof(request));
        IReadOnlyList<BillingCancellationTelegramCandidateModel> pending = await cancellations.GetPendingTelegramConfirmationsAsync(request.BatchSize, cancellationToken);
        int sent = 0;
        int skipped = 0;
        int failed = 0;
        foreach (BillingCancellationTelegramCandidateModel candidate in pending) {
            cancellationToken.ThrowIfCancellationRequested();
            try {
                TelegramMessageDeliveryResult result = await DeliverAsync(candidate, cancellationToken);
                switch (result) {
                    case TelegramMessageDeliveryResult.Sent: sent++; break;
                    case TelegramMessageDeliveryResult.RecipientUnavailable: skipped++; break;
                    default: failed++; break;
                }
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            } catch (Exception) {
                failed++;
                // Exceptions from HTTP transports can contain a credential-bearing request URL.
                logger.LogWarning("Could not deliver Telegram cancellation confirmation; the intent remains retryable.");
            }
        }
        return new BillingCancellationTelegramRunResult(sent, skipped, failed);
    }

    private async Task<TelegramMessageDeliveryResult> DeliverAsync(BillingCancellationTelegramCandidateModel candidate, CancellationToken cancellationToken) {
        await using IAsyncDisposable lease = await checkoutLock.AcquireAsync(candidate.UserId.Value, cancellationToken);
        (long Recipient, string Text)? notice = null;
        await transactions.ExecuteSerializedAsync(BillingOperationLockKeys.ForUser(candidate.UserId.Value), async token => {
            notice = null;
            BillingCancellationTelegramSnapshotModel? cancellation = await cancellations.GetTelegramConfirmationAsync(candidate.Id, candidate.UserId, token);
            if (cancellation is null || cancellation.HandledAtUtc is not null) {
                return;
            }
            UserBillingProfileModel? user = await sender.Send(new GetUserBillingProfileIncludingDeletedQuery(candidate.UserId), token);
            BillingSubscriptionOverviewReadModel? subscription = await subscriptions.GetOverviewReadModelByUserIdAsync(candidate.UserId, token);
            DateTime now = timeProvider.GetUtcNow().UtcDateTime;
            if (IsEligible(candidate, cancellation, user, subscription, now)) {
                notice = (user!.TelegramUserId!.Value,
                    TelegramCancellationConfirmationContent.Render(cancellation.EffectiveAtUtc, user.Language, user.TimeZoneId));
            } else {
                await cancellations.MarkTelegramConfirmationHandledAsync(candidate.Id, candidate.UserId, now, token);
            }
        }, cancellationToken);
        if (notice is not { } ready) {
            return TelegramMessageDeliveryResult.RecipientUnavailable;
        }
        // Never replay sendMessage inside a retryable database transaction.
        TelegramMessageDeliveryResult delivery = await messages.SendAsync(ready.Recipient, ready.Text, cancellationToken);
        if (delivery is TelegramMessageDeliveryResult.Retry) {
            return delivery;
        }
        await transactions.ExecuteSerializedAsync(BillingOperationLockKeys.ForUser(candidate.UserId.Value),
            token => cancellations.MarkTelegramConfirmationHandledAsync(candidate.Id, candidate.UserId, timeProvider.GetUtcNow().UtcDateTime, token),
            cancellationToken);
        return delivery;
    }

    private static bool IsEligible(BillingCancellationTelegramCandidateModel candidate, BillingCancellationTelegramSnapshotModel cancellation,
        UserBillingProfileModel? user, BillingSubscriptionOverviewReadModel? subscription, DateTime now) {
        if (cancellation is not { Provider: BillingProviderNames.TelegramStars, Outcome: "scheduled" } || cancellation.EffectiveAtUtc <= now ||
            user is not { IsActive: true, IsDeleted: false, HasPaidPremium: true, TelegramUserId: > 0 } || user.UserId != candidate.UserId ||
            subscription is not { Provider: BillingProviderNames.TelegramStars, CancelAtPeriodEnd: true, Status: "active" or "trialing" or "past_due" } ||
            subscription.UserId != candidate.UserId.Value || subscription.CurrentPeriodEndUtc != cancellation.EffectiveAtUtc ||
            !string.Equals(subscription.ExternalSubscriptionId, cancellation.ExternalSubscriptionId, StringComparison.Ordinal)) {
            return false;
        }
        string customer = user.TelegramUserId.Value.ToString(CultureInfo.InvariantCulture) + ":" + candidate.UserId.Value.ToString("N", CultureInfo.InvariantCulture);
        return string.Equals(subscription.ExternalCustomerId, customer, StringComparison.Ordinal);
    }
}
