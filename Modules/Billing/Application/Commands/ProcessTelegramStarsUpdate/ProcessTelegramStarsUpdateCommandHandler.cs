using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Common;
using FoodDiary.Modules.Billing.Application.Commands.RecoverStarsRefundCancellation;
using FoodDiary.Modules.Billing.Application.Services;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Billing.Domain.ValueObjects;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfileIncludingDeleted;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Commands.ProcessTelegramStarsUpdate;

public sealed class ProcessTelegramStarsUpdateCommandHandler(IBillingPaymentWriteRepository payments,
    IBillingSubscriptionWriteRepository subscriptions, IBillingWebhookEventWriteRepository events,
    IBillingTransactionRunner transactions, ISender sender, BillingAccessService access,
    IBillingPublicConfigProvider config, BillingCancellationRecorder cancellations, TimeProvider clock)
    : IRequestHandler<ProcessTelegramStarsUpdateCommand, Result<TelegramStarsUpdateResultModel>> {
    private const string Provider = BillingProviderNames.TelegramStars;

    public async Task<Result<TelegramStarsUpdateResultModel>> Handle(ProcessTelegramStarsUpdateCommand request, CancellationToken cancellationToken) {
        TelegramStarsUpdateModel update = request.Update;
        if (update.UpdateId < 0 || update.TelegramUserId <= 0 || update.InvoicePayload.Length is < 1 or > 128 ||
            update.Kind is not ("pre_checkout" or "payment" or "refund" or "subscription")) {
            return Failure("Invalid Telegram payment update.");
        }
        BillingPayment? before = await payments.GetByExternalPaymentIdAsync(Provider, update.InvoicePayload, cancellationToken);
        if (before is null || before.Kind is not BillingPaymentKinds.Checkout) {
            return update.Kind is "pre_checkout" ? Reject() : Failure("Unknown Telegram invoice.");
        }
        var result = Result.Success(TelegramStarsUpdateResultModel.UpdateProcessed);
        string eventId = "stars-update:" + update.UpdateId.ToString(CultureInfo.InvariantCulture);
        try {
            await transactions.ExecuteSerializedAsync(BillingOperationLockKeys.ForUser(before.UserId.Value), async token => {
                result = Result.Success(TelegramStarsUpdateResultModel.UpdateProcessed);
                BillingPayment order = await payments.GetByExternalPaymentIdAsync(Provider, update.InvoicePayload, token)
                    ?? throw new InvalidOperationException("Telegram invoice no longer exists.");
                string customerId = update.TelegramUserId.ToString(CultureInfo.InvariantCulture) + ":" + order.UserId.Value.ToString("N", CultureInfo.InvariantCulture);
                if (!string.Equals(order.ExternalCustomerId, customerId, StringComparison.Ordinal)) {
                    result = update.Kind is "pre_checkout" ? Reject() : Failure("Telegram invoice owner mismatch.");
                    return;
                }
                BillingSubscription? current = await subscriptions.GetByUserIdAsync(order.UserId, token);
                UserBillingProfileModel? user = await sender.Send(new GetUserBillingProfileIncludingDeletedQuery(order.UserId), token);
                DateTime now = clock.GetUtcNow().UtcDateTime;
                if (update.Kind is "pre_checkout") {
                    result = await ApproveAsync(update, order, current, user, eventId, now, token);
                    return;
                }
                if (await events.ExistsAsync(Provider, eventId, token)) {
                    return;
                }
                bool ownsCurrent = current is { Provider: Provider } && string.Equals(current.ExternalSubscriptionId, update.InvoicePayload, StringComparison.Ordinal);
                if (update.Kind is "payment" or "refund") {
                    if (update.Currency is not "XTR" || update.TotalAmount is not > 0 || update.TotalAmount != order.Amount ||
                        string.IsNullOrWhiteSpace(update.PaymentChargeId) || update.PaymentChargeId.Length > 255 ||
                        update.PaymentOccurredAtUtc is not { Kind: DateTimeKind.Utc }) {
                        result = Failure("Invalid Telegram payment observation.");
                        return;
                    }
                    result = await RecordPaymentAsync(update, order, ownsCurrent ? current : null, user, eventId, now, token);
                } else if (ownsCurrent && current is not null && IsNewerState(update, current, now)) {
                    if (update.SubscriptionState is not ("canceled" or "active" or "failed")) {
                        result = Failure("Unknown Telegram subscription state.");
                        return;
                    }
                    Apply(current, update.SubscriptionState is "failed" ? "past_due" : current.Status,
                        current.CurrentPeriodStartUtc, current.CurrentPeriodEndUtc,
                        update.SubscriptionState is "canceled" || (update.SubscriptionState is "failed" && current.CancelAtPeriodEnd),
                        current.ExternalPaymentMethodId, eventId, now);
                    await subscriptions.UpdateAsync(current, token);
                    await SynchronizeAccessAsync(current, user, token);
                }
                if (result.IsSuccess) {
                    // Persist only the bounded financial envelope, never raw Telegram messages or credentials.
                    await events.AddAsync(BillingWebhookEvent.CreateProcessed(Provider, eventId, "stars." + update.Kind,
                        update.PaymentChargeId ?? update.InvoicePayload, now, JsonSerializer.Serialize(update)), token);
                }
            }, cancellationToken);
        } catch (BillingWebhookEventAlreadyProcessedException) {
            // The durable refund intent may still need its provider cancellation after a concurrent receipt.
            result = Result.Success(TelegramStarsUpdateResultModel.UpdateProcessed);
        }
        if (result.IsSuccess && update.Kind is not "pre_checkout") {
            BillingSubscription? pending = await subscriptions.GetByUserIdAsync(before.UserId, cancellationToken);
            if (pending is { Provider: Provider, ExternalPaymentMethodId: not null } &&
                string.Equals(pending.ExternalSubscriptionId, update.InvoicePayload, StringComparison.Ordinal) &&
                string.Equals(pending.ProviderMetadataJson, TelegramStarsBillingState.RefundCancellationPending, StringComparison.Ordinal)) {
                // Revoke access durably before provider HTTP. A failed/lost cancellation retries on redelivery.
                Result<Unit> canceled = await sender.Send(new RecoverStarsRefundCancellationCommand(before.UserId), cancellationToken);
                if (canceled.IsFailure) {
                    return Result.Failure<TelegramStarsUpdateResultModel>(canceled.Error);
                }
            }
        }
        return result;
    }

    private async Task<Result<TelegramStarsUpdateResultModel>> ApproveAsync(TelegramStarsUpdateModel update, BillingPayment order,
        BillingSubscription? current, UserBillingProfileModel? user, string eventId, DateTime now, CancellationToken token) {
        if (!TelegramStarsInvoiceMetadata.TryReadPeriod(order.ProviderMetadataJson, out _) ||
            user is not { IsActive: true, IsDeleted: false } || user.TelegramUserId != update.TelegramUserId || user.HasPaidPremium ||
            current is not { Provider: Provider, Status: BillingSubscription.PendingCheckoutStatus } ||
            (current.ExternalSubscriptionId is { } selected && !string.Equals(selected, update.InvoicePayload, StringComparison.Ordinal)) ||
            (current.ExternalSubscriptionId is not null && !string.Equals(current.LastWebhookEventId, eventId, StringComparison.Ordinal)) ||
            order.CreatedOnUtc <= now.AddMinutes(-15) || update.Currency is not "XTR" || order.Currency is not "XTR" ||
            update.TotalAmount is not > 0 || update.TotalAmount != order.Amount ||
            !config.GetPublicConfig().AvailableProviders.Contains(Provider, StringComparer.OrdinalIgnoreCase)) {
            return Reject();
        }
        // Reserve exactly one invoice under the same per-user lock used by checkout and payments.
        current.ApplyProviderSnapshot(Provider, update.InvoicePayload, externalPaymentMethodId: null, order.ExternalPriceId, "monthly",
            BillingSubscription.PendingCheckoutStatus, currentPeriodStartUtc: null, currentPeriodEndUtc: null, cancelAtPeriodEnd: false, canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null, eventId, now, webhookOccurredAtUtc: now);
        await subscriptions.UpdateAsync(current, token);
        return Result.Success(TelegramStarsUpdateResultModel.CheckoutAccepted);
    }

    private async Task<Result<TelegramStarsUpdateResultModel>> RecordPaymentAsync(TelegramStarsUpdateModel update, BillingPayment order,
        BillingSubscription? current, UserBillingProfileModel? user, string eventId, DateTime now, CancellationToken token) {
        if (!TelegramStarsInvoiceMetadata.TryReadPeriod(order.ProviderMetadataJson, out int periodSeconds)) {
            return Failure("Invalid saved Telegram subscription period.");
        }
        string charge = update.PaymentChargeId!;
        string refundId = "refund:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(charge)));
        string paymentId = update.Kind is "refund" ? refundId : charge;
        if (await payments.GetByExternalPaymentIdAsync(Provider, paymentId, token) is not null) {
            return Result.Success(TelegramStarsUpdateResultModel.UpdateProcessed);
        }
        BillingPayment? original = await payments.GetByExternalPaymentIdAsync(Provider, charge, token);
        BillingPayment? refund = await payments.GetByExternalPaymentIdAsync(Provider, refundId, token);
        DateTime? end = update.Kind is "payment" ? update.SubscriptionExpirationUtc : original?.CurrentPeriodEndUtc;
        if (update.Kind is "payment" && (!update.IsRecurring || end is not { Kind: DateTimeKind.Utc } ||
            end <= DateTime.UnixEpoch.AddSeconds(periodSeconds) || end <= update.PaymentOccurredAtUtc)) {
            return Failure("A confirmed recurring payment and expiration are required.");
        }
        DateTime? start = update.Kind is "refund" ? original?.CurrentPeriodStartUtc : null;
        start ??= end?.AddSeconds(-periodSeconds);
        string transactionKind = update.IsFirstRecurring ? BillingPaymentKinds.Transaction : BillingPaymentKinds.Renewal;
        var payment = BillingPayment.CreateWithFinancials(order.UserId, order.SubscriptionReference, Provider, paymentId,
            order.ExternalCustomerId, update.InvoicePayload, externalPaymentMethodId: null, order.ExternalPriceId, "monthly",
            update.Kind is "refund" ? "refunded" : "active",
            update.Kind is "refund" ? BillingPaymentKinds.Refund : transactionKind,
            BillingPaymentFinancials.FromFields(update.Kind is "refund" ? -update.TotalAmount : update.TotalAmount, "XTR"),
            start, end, eventId, providerMetadataJson: null, update.PaymentOccurredAtUtc);
        await payments.AddAsync(payment, token);
        if (current is null) {
            return Result.Success(TelegramStarsUpdateResultModel.UpdateProcessed);
        }
        if (update.Kind is "payment" && refund is null && end > (current.CurrentPeriodEndUtc ?? DateTime.MinValue)) {
            // A confirmed provider expiration wins over server time and calendar arithmetic.
            Apply(current, "active", start, end, current.CancelAtPeriodEnd,
                current.ExternalPaymentMethodId ?? (update.IsFirstRecurring ? charge : null), eventId, now);
        } else if (update.Kind is "refund" && (original?.CurrentPeriodEndUtc == current.CurrentPeriodEndUtc ||
            (original is null && string.Equals(current.ExternalPaymentMethodId, charge, StringComparison.Ordinal)))) {
            Apply(current, "refunded", current.CurrentPeriodStartUtc, current.CurrentPeriodEndUtc, current.CancelAtPeriodEnd,
                current.ExternalPaymentMethodId ?? (current.Status is BillingSubscription.PendingCheckoutStatus ? charge : null),
                eventId, now, TelegramStarsBillingState.RefundCancellationPending);
        } else {
            return Result.Success(TelegramStarsUpdateResultModel.UpdateProcessed);
        }
        await subscriptions.UpdateAsync(current, token);
        await SynchronizeAccessAsync(current, user, token);
        return Result.Success(TelegramStarsUpdateResultModel.UpdateProcessed);
    }

    private async Task SynchronizeAccessAsync(BillingSubscription current, UserBillingProfileModel? user, CancellationToken token) {
        if (user is { IsActive: true, IsDeleted: false }) {
            await access.EnsurePremiumRoleAsync(user, current,
                access.ShouldHavePremiumAccess(current.Status, current.CurrentPeriodEndUtc, Provider), token);
        } else {
            await access.RevokeManagedPremiumRoleAsync(current, token);
        }
        await cancellations.SynchronizeAsync(current, "telegram", token);
    }

    private static void Apply(BillingSubscription current, string status, DateTime? start, DateTime? end,
        bool canceled, string? firstCharge, string eventId, DateTime now, string? metadata = null) =>
        current.ApplyProviderSnapshot(Provider, current.ExternalSubscriptionId, firstCharge, current.ExternalPriceId, "monthly",
            status, start, end, canceled, status is "canceled" ? now : null, trialStartUtc: null, trialEndUtc: null,
            eventId, now, metadata ?? current.ProviderMetadataJson, webhookOccurredAtUtc: now);

    private static bool IsNewerState(TelegramStarsUpdateModel update, BillingSubscription current, DateTime now) =>
        current.LastSyncedAtUtc < now.AddDays(-7) ||
        current.LastWebhookEventId is not { } id || !id.StartsWith("stars-update:", StringComparison.Ordinal) ||
        !long.TryParse(id.AsSpan("stars-update:".Length), CultureInfo.InvariantCulture, out long previous) || update.UpdateId > previous;

    private static Result<TelegramStarsUpdateResultModel> Reject() => Result.Success<TelegramStarsUpdateResultModel>(
        new TelegramStarsUpdateResultModel.Rejected("This invoice is unavailable. Open FoodDiary and create a new checkout."));

    private static Result<TelegramStarsUpdateResultModel> Failure(string message) =>
        Result.Failure<TelegramStarsUpdateResultModel>(BillingErrors.WebhookValidationFailed(message));
}
