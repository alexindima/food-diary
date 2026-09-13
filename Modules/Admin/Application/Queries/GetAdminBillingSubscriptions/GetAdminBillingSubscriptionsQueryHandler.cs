using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Admin.Application.Services;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminBillingSubscriptions;

public sealed class GetAdminBillingSubscriptionsQueryHandler(IAdminBillingReadRepository billingRepository)
    : IQueryHandler<GetAdminBillingSubscriptionsQuery, Result<PagedResponse<AdminBillingSubscriptionReadModel>>> {
    public async Task<Result<PagedResponse<AdminBillingSubscriptionReadModel>>> Handle(GetAdminBillingSubscriptionsQuery query, CancellationToken cancellationToken) {
        AdminBillingListFilter filter = AdminBillingQueryFilters.Create(
            query.Page,
            query.Limit,
            query.Provider,
            query.Status,
            kind: null,
            query.Search,
            query.FromUtc,
            query.ToUtc);
        (IReadOnlyList<AdminBillingSubscriptionReadModel> items, int totalItems) =
            await billingRepository.GetSubscriptionsAsync(filter, cancellationToken).ConfigureAwait(false);

        return Result.Success(AdminBillingQueryFilters.ToPagedResponse(items, filter.Page, filter.Limit, totalItems));
    }
}
