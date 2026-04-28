using FoodDiary.Application.Abstractions.Admin.Models;

namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IAdminBillingRepository {
    Task<(IReadOnlyList<AdminBillingSubscriptionReadModel> Items, int TotalItems)> GetSubscriptionsAsync(
        AdminBillingListFilter filter,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AdminBillingPaymentReadModel> Items, int TotalItems)> GetPaymentsAsync(
        AdminBillingListFilter filter,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AdminBillingWebhookEventReadModel> Items, int TotalItems)> GetWebhookEventsAsync(
        AdminBillingListFilter filter,
        CancellationToken cancellationToken = default);
}
