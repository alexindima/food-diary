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
        var filter = AdminBillingQueryFilters.Create(
            query.Page,
            query.Limit,
            query.Provider,
            query.Status,
            null,
            query.Search,
            query.FromUtc,
            query.ToUtc);
        var pageData = await billingRepository.GetSubscriptionsAsync(filter, cancellationToken);
        return Result.Success(ToPagedResponse(pageData.Items, filter.Page, filter.Limit, pageData.TotalItems));
    }

    private static PagedResponse<T> ToPagedResponse<T>(IReadOnlyList<T> items, int page, int limit, int totalItems) {
        var totalPages = (int)Math.Ceiling(totalItems / (double)limit);
        return new PagedResponse<T>(items, page, limit, totalPages, totalItems);
    }
}
