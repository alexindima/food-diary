using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
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
            query = query.Where(item => item.subscription.Provider == filter.Provider);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status)) {
            query = query.Where(item => item.subscription.Status == filter.Status);
        }

        if (filter.FromUtc.HasValue) {
            query = query.Where(item => item.subscription.CreatedOnUtc >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue) {
            query = query.Where(item => item.subscription.CreatedOnUtc <= filter.ToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search)) {
            var term = $"%{EscapeLikePattern(filter.Search)}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.user.Email, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.subscription.ExternalCustomerId, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.subscription.ExternalSubscriptionId ?? string.Empty, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.subscription.ExternalPaymentMethodId ?? string.Empty, term, LikeEscapeCharacter));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.subscription.CreatedOnUtc)
            .Skip((filter.Page - 1) * filter.Limit)
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
            .ToListAsync(cancellationToken);

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
            query = query.Where(item => item.payment.Provider == filter.Provider);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status)) {
            query = query.Where(item => item.payment.Status == filter.Status);
        }

        if (!string.IsNullOrWhiteSpace(filter.Kind)) {
            query = query.Where(item => item.payment.Kind == filter.Kind);
        }

        if (filter.FromUtc.HasValue) {
            query = query.Where(item => item.payment.CreatedOnUtc >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue) {
            query = query.Where(item => item.payment.CreatedOnUtc <= filter.ToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search)) {
            var term = $"%{EscapeLikePattern(filter.Search)}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.user.Email, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.payment.ExternalPaymentId, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.payment.ExternalCustomerId ?? string.Empty, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.payment.ExternalSubscriptionId ?? string.Empty, term, LikeEscapeCharacter));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.payment.CreatedOnUtc)
            .Skip((filter.Page - 1) * filter.Limit)
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
                item.payment.ModifiedOnUtc))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<AdminBillingWebhookEventReadModel> Items, int TotalItems)> GetWebhookEventsAsync(
        AdminBillingListFilter filter,
        CancellationToken cancellationToken = default) {
        var query = context.BillingWebhookEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Provider)) {
            query = query.Where(webhookEvent => webhookEvent.Provider == filter.Provider);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status)) {
            query = query.Where(webhookEvent => webhookEvent.Status == filter.Status);
        }

        if (filter.FromUtc.HasValue) {
            query = query.Where(webhookEvent => webhookEvent.ProcessedAtUtc >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue) {
            query = query.Where(webhookEvent => webhookEvent.ProcessedAtUtc <= filter.ToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search)) {
            var term = $"%{EscapeLikePattern(filter.Search)}%";
            query = query.Where(webhookEvent =>
                EF.Functions.ILike(webhookEvent.EventId, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(webhookEvent.EventType, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(webhookEvent.ExternalObjectId ?? string.Empty, term, LikeEscapeCharacter));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(webhookEvent => webhookEvent.ProcessedAtUtc)
            .Skip((filter.Page - 1) * filter.Limit)
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
                webhookEvent.ModifiedOnUtc))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    private static string EscapeLikePattern(string value) {
        return value
            .Trim()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
