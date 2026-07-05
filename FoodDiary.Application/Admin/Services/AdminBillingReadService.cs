using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Services;

public sealed class AdminBillingReadService(IAdminBillingReadRepository billingRepository) : IAdminBillingReadService {
    public async Task<Result<PagedResponse<AdminBillingSubscriptionReadModel>>> GetSubscriptionsAsync(
        int page,
        int limit,
        string? provider,
        string? status,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken) {
        AdminBillingListFilter filter = AdminBillingQueryFilters.Create(
            page,
            limit,
            provider,
            status,
            kind: null,
            search,
            fromUtc,
            toUtc);
        (IReadOnlyList<AdminBillingSubscriptionReadModel> items, int totalItems) =
            await billingRepository.GetSubscriptionsAsync(filter, cancellationToken).ConfigureAwait(false);

        return Result.Success(ToPagedResponse(items, filter.Page, filter.Limit, totalItems));
    }

    public async Task<Result<PagedResponse<AdminBillingPaymentReadModel>>> GetPaymentsAsync(
        int page,
        int limit,
        string? provider,
        string? status,
        string? kind,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken) {
        AdminBillingListFilter filter = AdminBillingQueryFilters.Create(
            page,
            limit,
            provider,
            status,
            kind,
            search,
            fromUtc,
            toUtc);
        (IReadOnlyList<AdminBillingPaymentReadModel> items, int totalItems) =
            await billingRepository.GetPaymentsAsync(filter, cancellationToken).ConfigureAwait(false);

        return Result.Success(ToPagedResponse(items, filter.Page, filter.Limit, totalItems));
    }

    public async Task<Result<PagedResponse<AdminBillingWebhookEventReadModel>>> GetWebhookEventsAsync(
        int page,
        int limit,
        string? provider,
        string? status,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken) {
        AdminBillingListFilter filter = AdminBillingQueryFilters.Create(
            page,
            limit,
            provider,
            status,
            kind: null,
            search,
            fromUtc,
            toUtc);
        (IReadOnlyList<AdminBillingWebhookEventReadModel> items, int totalItems) =
            await billingRepository.GetWebhookEventsAsync(filter, cancellationToken).ConfigureAwait(false);

        return Result.Success(ToPagedResponse(items, filter.Page, filter.Limit, totalItems));
    }

    private static PagedResponse<T> ToPagedResponse<T>(IReadOnlyList<T> items, int page, int limit, int totalItems) {
        int totalPages = (int)Math.Ceiling(totalItems / (double)limit);
        return new PagedResponse<T>(items, page, limit, totalPages, totalItems);
    }
}
