using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminBillingSubscriptions;

public sealed class GetAdminBillingSubscriptionsQueryHandler(IAdminBillingRepository billingRepository)
    : IQueryHandler<GetAdminBillingSubscriptionsQuery, Result<PagedResponse<AdminBillingSubscriptionReadModel>>> {
    public async Task<Result<PagedResponse<AdminBillingSubscriptionReadModel>>> Handle(
        GetAdminBillingSubscriptionsQuery query,
        CancellationToken cancellationToken) {
        var filter = ToFilter(query.Page, query.Limit, query.Provider, query.Status, null, query.Search, query.FromUtc, query.ToUtc);
        var pageData = await billingRepository.GetSubscriptionsAsync(filter, cancellationToken);
        return Result.Success(ToPagedResponse(pageData.Items, filter.Page, filter.Limit, pageData.TotalItems));
    }

    private static AdminBillingListFilter ToFilter(
        int page,
        int limit,
        string? provider,
        string? status,
        string? kind,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc) {
        return new AdminBillingListFilter(
            page <= 0 ? 1 : page,
            limit is > 0 and <= 100 ? limit : 20,
            Normalize(provider),
            Normalize(status),
            Normalize(kind),
            Normalize(search),
            NormalizeUtc(fromUtc),
            NormalizeUtc(toUtc));
    }

    private static PagedResponse<T> ToPagedResponse<T>(IReadOnlyList<T> items, int page, int limit, int totalItems) {
        var totalPages = (int)Math.Ceiling(totalItems / (double)limit);
        return new PagedResponse<T>(items, page, limit, totalPages, totalItems);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime? NormalizeUtc(DateTime? value) =>
        value?.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value?.ToUniversalTime();
}
