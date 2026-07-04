using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminBillingSubscriptions;

public sealed class GetAdminBillingSubscriptionsQueryHandler(IAdminBillingReadRepository billingRepository)
    : IQueryHandler<GetAdminBillingSubscriptionsQuery, Result<PagedResponse<AdminBillingSubscriptionReadModel>>> {
    public async Task<Result<PagedResponse<AdminBillingSubscriptionReadModel>>> Handle(
        GetAdminBillingSubscriptionsQuery query,
        CancellationToken cancellationToken) {
        AdminBillingListFilter filter = AdminBillingQueryFilters.Create(
            query.Page,
            query.Limit,
            query.Provider,
            query.Status,
            kind: null,
            query.Search,
            query.FromUtc,
            query.ToUtc);
        (IReadOnlyList<AdminBillingSubscriptionReadModel> Items, int TotalItems) = await billingRepository.GetSubscriptionsAsync(filter, cancellationToken).ConfigureAwait(false);
        return Result.Success(ToPagedResponse(Items, filter.Page, filter.Limit, TotalItems));
    }

    private static PagedResponse<T> ToPagedResponse<T>(IReadOnlyList<T> items, int page, int limit, int totalItems) {
        int totalPages = (int)Math.Ceiling(totalItems / (double)limit);
        return new PagedResponse<T>(items, page, limit, totalPages, totalItems);
    }
}
