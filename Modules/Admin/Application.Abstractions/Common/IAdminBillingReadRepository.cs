using FoodDiary.Modules.Admin.Application.Abstractions.Models;

namespace FoodDiary.Modules.Admin.Application.Abstractions.Common;

public interface IAdminBillingReadRepository {
    Task<(IReadOnlyList<AdminBillingSubscriptionReadModel> Items, int TotalItems)> GetSubscriptionsAsync(
        AdminBillingListFilter filter,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AdminBillingPaymentReadModel> Items, int TotalItems)> GetPaymentsAsync(
        AdminBillingListFilter filter,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AdminBillingWebhookEventReadModel> Items, int TotalItems)> GetWebhookEventsAsync(
        AdminBillingListFilter filter,
        CancellationToken cancellationToken = default);

    Task<AdminBillingRevenueSummaryReadModel> GetRevenueSummaryAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}
