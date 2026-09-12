using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Admin;

public sealed class AdminBillingRepository(FoodDiaryDbContext context) : IAdminBillingRepository {
    private const string LikeEscapeCharacter = "\\";

    public async Task<(IReadOnlyList<AdminBillingSubscriptionReadModel> Items, int TotalItems)> GetSubscriptionsAsync(
        AdminBillingListFilter filter,
        CancellationToken cancellationToken = default) {
        var query =
            from subscription in context.BillingSubscriptions.AsNoTracking()
            join user in context.Users.AsNoTracking() on subscription.UserId equals user.Id
            select new { subscription, user };

        if (!string.IsNullOrWhiteSpace(filter.Provider)) {
            query = query.AsNoTracking().Where(item => item.subscription.Provider == filter.Provider);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status)) {
            query = query.AsNoTracking().Where(item => item.subscription.Status == filter.Status);
        }

        if (filter.FromUtc.HasValue) {
            query = query.AsNoTracking().Where(item => item.subscription.CreatedOnUtc >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue) {
            query = query.AsNoTracking().Where(item => item.subscription.CreatedOnUtc <= filter.ToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search)) {
            string term = BuildSearchPattern(filter.Search);
            bool hasId = Guid.TryParse(filter.Search.Trim(), out Guid recordId);
            var userId = new UserId(recordId);
            query = query.AsNoTracking().Where(item =>
                (hasId && (item.subscription.Id == recordId || item.user.Id == userId)) ||
                (item.user.Email != null && EF.Functions.ILike(item.user.Email, term, LikeEscapeCharacter)) ||
                EF.Functions.ILike(item.subscription.ExternalCustomerId, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.subscription.ExternalSubscriptionId ?? string.Empty, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.subscription.ExternalPaymentMethodId ?? string.Empty, term, LikeEscapeCharacter));
        }

        int total = await query.AsNoTracking().CountAsync(cancellationToken).ConfigureAwait(false);
        List<AdminBillingSubscriptionReadModel> items = await query.AsNoTracking()
            .OrderByDescending(item => item.subscription.CreatedOnUtc).ThenByDescending(item => item.subscription.Id)
            .Skip(GetSkipCount(filter))
            .Take(filter.Limit)
            .Select(item => new AdminBillingSubscriptionReadModel(
                item.subscription.Id,
                item.user.Id.Value,
                item.user.Email,
                item.subscription.Provider,
                item.subscription.ExternalCustomerId,
                item.subscription.ExternalSubscriptionId,
                item.subscription.ExternalPaymentMethodId,
                item.subscription.ExternalPriceId,
                item.subscription.Plan,
                item.subscription.Status,
                item.subscription.CurrentPeriodStartUtc,
                item.subscription.CurrentPeriodEndUtc,
                item.subscription.CancelAtPeriodEnd,
                item.subscription.NextBillingAttemptUtc,
                item.subscription.LastWebhookEventId,
                item.subscription.LastSyncedAtUtc,
                item.subscription.CreatedOnUtc,
                item.subscription.ModifiedOnUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items, total);
    }

    public async Task<(IReadOnlyList<AdminBillingPaymentReadModel> Items, int TotalItems)> GetPaymentsAsync(
        AdminBillingListFilter filter,
        CancellationToken cancellationToken = default) {
        var query =
            from payment in context.BillingPayments.AsNoTracking()
            join user in context.Users.AsNoTracking() on payment.UserId equals user.Id
            select new { payment, user };
        if (!string.IsNullOrWhiteSpace(filter.Provider)) {
            query = query.AsNoTracking().Where(item => item.payment.Provider == filter.Provider);
        }
        if (!string.IsNullOrWhiteSpace(filter.Status)) {
            query = query.AsNoTracking().Where(item => item.payment.Status == filter.Status);
        }
        if (!string.IsNullOrWhiteSpace(filter.Kind)) {
            query = query.AsNoTracking().Where(item => item.payment.Kind == filter.Kind);
        }
        if (filter.FromUtc.HasValue) {
            query = query.AsNoTracking().Where(item => item.payment.CreatedOnUtc >= filter.FromUtc.Value);
        }
        if (filter.ToUtc.HasValue) {
            query = query.AsNoTracking().Where(item => item.payment.CreatedOnUtc <= filter.ToUtc.Value);
        }
        if (!string.IsNullOrWhiteSpace(filter.Search)) {
            string term = BuildSearchPattern(filter.Search);
            bool hasId = Guid.TryParse(filter.Search.Trim(), out Guid recordId);
            var userId = new UserId(recordId);
            query = query.AsNoTracking().Where(item =>
                (hasId && (item.payment.Id == recordId || item.payment.BillingSubscriptionId == recordId || item.user.Id == userId)) ||
                (item.user.Email != null && EF.Functions.ILike(item.user.Email, term, LikeEscapeCharacter)) ||
                EF.Functions.ILike(item.payment.ExternalPaymentId, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.payment.ExternalCustomerId ?? string.Empty, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.payment.ExternalSubscriptionId ?? string.Empty, term, LikeEscapeCharacter));
        }
        int total = await query.AsNoTracking().CountAsync(cancellationToken).ConfigureAwait(false);
        List<AdminBillingPaymentReadModel> items = await query.AsNoTracking()
            .OrderByDescending(item => item.payment.CreatedOnUtc).ThenByDescending(item => item.payment.Id)
            .Skip(GetSkipCount(filter))
            .Take(filter.Limit)
            .Select(item => new AdminBillingPaymentReadModel(
                item.payment.Id,
                item.user.Id.Value,
                item.user.Email,
                item.payment.BillingSubscriptionId,
                item.payment.Provider,
                item.payment.ExternalPaymentId,
                item.payment.ExternalCustomerId,
                item.payment.ExternalSubscriptionId,
                item.payment.ExternalPaymentMethodId,
                item.payment.ExternalPriceId,
                item.payment.Plan,
                item.payment.Status,
                item.payment.Kind,
                item.payment.Amount,
                item.payment.Currency,
                item.payment.CurrentPeriodStartUtc,
                item.payment.CurrentPeriodEndUtc,
                item.payment.WebhookEventId,
                item.payment.ProviderMetadataJson,
                item.payment.CreatedOnUtc,
                item.payment.ModifiedOnUtc,
                item.payment.Tax, item.payment.Fee, item.payment.Earnings,
                item.payment.PayoutCurrency, item.payment.PayoutEarnings))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return (items, total);
    }

    public async Task<(IReadOnlyList<AdminBillingWebhookEventReadModel> Items, int TotalItems)> GetWebhookEventsAsync(
        AdminBillingListFilter filter,
        CancellationToken cancellationToken = default) {
        IQueryable<BillingWebhookEvent> query = context.BillingWebhookEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Provider)) {
            query = query.AsNoTracking().Where(webhookEvent => webhookEvent.Provider == filter.Provider);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status)) {
            query = query.AsNoTracking().Where(webhookEvent => webhookEvent.Status == filter.Status);
        }

        if (filter.FromUtc.HasValue) {
            query = query.AsNoTracking().Where(webhookEvent => webhookEvent.ReceivedAtUtc >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue) {
            query = query.AsNoTracking().Where(webhookEvent => webhookEvent.ReceivedAtUtc <= filter.ToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search)) {
            string term = BuildSearchPattern(filter.Search);
            bool hasId = Guid.TryParse(filter.Search.Trim(), out Guid recordId);
            query = query.AsNoTracking().Where(webhookEvent =>
                (hasId && webhookEvent.Id == recordId) ||
                EF.Functions.ILike(webhookEvent.EventId, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(webhookEvent.EventType, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(webhookEvent.ExternalObjectId ?? string.Empty, term, LikeEscapeCharacter));
        }

        int total = await query.AsNoTracking().CountAsync(cancellationToken).ConfigureAwait(false);
        List<AdminBillingWebhookEventReadModel> items = await query.AsNoTracking()
            .OrderByDescending(webhookEvent => webhookEvent.ReceivedAtUtc).ThenByDescending(webhookEvent => webhookEvent.Id)
            .Skip(GetSkipCount(filter))
            .Take(filter.Limit)
            .Select(webhookEvent => new AdminBillingWebhookEventReadModel(
                webhookEvent.Id,
                webhookEvent.Provider,
                webhookEvent.EventId,
                webhookEvent.EventType,
                webhookEvent.ExternalObjectId,
                webhookEvent.Status,
                webhookEvent.ProcessedAtUtc,
                webhookEvent.PayloadJson,
                webhookEvent.ErrorMessage,
                webhookEvent.CreatedOnUtc,
                webhookEvent.ModifiedOnUtc,
                webhookEvent.ReceivedAtUtc,
                webhookEvent.AttemptCount,
                webhookEvent.NextAttemptAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items, total);
    }

    public async Task<AdminBillingRevenueSummaryReadModel> GetRevenueSummaryAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default) {
        var rows = await context.BillingPayments
            .AsNoTracking()
            .Where(payment =>
                (payment.OccurredAtUtc ?? payment.CreatedOnUtc) >= fromUtc &&
                (payment.OccurredAtUtc ?? payment.CreatedOnUtc) < toUtc &&
                payment.Amount != null &&
                payment.Currency != null &&
                (payment.Kind == BillingPaymentKinds.Transaction ||
                    payment.Kind == BillingPaymentKinds.Refund ||
                    payment.Kind == BillingPaymentKinds.Credit ||
                    payment.Kind == BillingPaymentKinds.Chargeback ||
                    payment.Kind == BillingPaymentKinds.ChargebackReverse ||
                    payment.Kind == BillingPaymentKinds.CreditReverse))
            .GroupBy(payment => payment.Currency!)
            .Select(group => new {
                Currency = group.Key,
                Gross = group.Where(payment =>
                        payment.Kind == BillingPaymentKinds.Transaction && payment.Status == "completed")
                    .Sum(payment => payment.Amount ?? 0m),
                Refunds = -group.Where(payment =>
                        payment.Kind == BillingPaymentKinds.Refund || payment.Kind == BillingPaymentKinds.Credit)
                    .Sum(payment => payment.Amount ?? 0m),
                Chargebacks = -group.Where(payment => payment.Kind == BillingPaymentKinds.Chargeback)
                    .Sum(payment => payment.Amount ?? 0m),
                Reversals = group.Where(payment =>
                        payment.Kind == BillingPaymentKinds.ChargebackReverse || payment.Kind == BillingPaymentKinds.CreditReverse)
                    .Sum(payment => payment.Amount ?? 0m),
                SuccessfulPayments = group.Count(payment =>
                    payment.Kind == BillingPaymentKinds.Transaction && payment.Status == "completed"),
                Tax = group.Where(payment =>
                        payment.Kind == BillingPaymentKinds.Transaction && payment.Status == "completed")
                    .Sum(payment => payment.Tax ?? 0m),
                PaddleFees = group.Sum(payment => payment.Fee ?? 0m),
                PaddleEarnings = group.Sum(payment => payment.Earnings ?? 0m),
                EarningsTrackedPayments = group.Count(payment =>
                    payment.Kind == BillingPaymentKinds.Transaction &&
                    payment.Status == "completed" &&
                    payment.Earnings != null),
            })
            .OrderBy(row => row.Currency)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        int renewalRecords = await context.BillingPayments.AsNoTracking().CountAsync(payment =>
            payment.Kind == BillingPaymentKinds.Renewal && (payment.OccurredAtUtc ?? payment.CreatedOnUtc) >= fromUtc &&
            (payment.OccurredAtUtc ?? payment.CreatedOnUtc) < toUtc, cancellationToken).ConfigureAwait(false);
        int cancellations = await context.BillingSubscriptions.AsNoTracking().CountAsync(subscription =>
            subscription.CancelAtPeriodEnd && subscription.CurrentPeriodEndUtc >= fromUtc && subscription.CurrentPeriodEndUtc < toUtc,
            cancellationToken).ConfigureAwait(false);
        return new AdminBillingRevenueSummaryReadModel(
            fromUtc,
            toUtc,
            [.. rows.Select(row => new AdminBillingRevenueCurrencyReadModel(
                row.Currency,
                row.Gross,
                row.Refunds,
                row.Chargebacks,
                row.Reversals,
                row.Gross - row.Refunds - row.Chargebacks + row.Reversals,
                row.SuccessfulPayments,
                row.Tax,
                row.PaddleFees,
                row.PaddleEarnings,
                row.EarningsTrackedPayments))], renewalRecords, cancellations);
    }

    private static int GetSkipCount(AdminBillingListFilter filter) =>
        (filter.Page - 1) * filter.Limit;

    private static string BuildSearchPattern(string value) =>
        $"%{EscapeLikePattern(value)}%";

    private static string EscapeLikePattern(string value) {
        return value
            .Trim()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
