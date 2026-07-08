using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Results;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Common;

public interface IAdminBillingReadService {
    Task<Result<PagedResponse<AdminBillingSubscriptionReadModel>>> GetSubscriptionsAsync(
        int page,
        int limit,
        string? provider,
        string? status,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken);

    Task<Result<PagedResponse<AdminBillingPaymentReadModel>>> GetPaymentsAsync(
        int page,
        int limit,
        string? provider,
        string? status,
        string? kind,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken);

    Task<Result<PagedResponse<AdminBillingWebhookEventReadModel>>> GetWebhookEventsAsync(
        int page,
        int limit,
        string? provider,
        string? status,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken);
}
