using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Results;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminBillingSubscriptions;

public sealed class GetAdminBillingSubscriptionsQueryHandler(IAdminBillingReadService readService)
    : IQueryHandler<GetAdminBillingSubscriptionsQuery, Result<PagedResponse<AdminBillingSubscriptionReadModel>>> {
    public async Task<Result<PagedResponse<AdminBillingSubscriptionReadModel>>> Handle(
        GetAdminBillingSubscriptionsQuery query,
        CancellationToken cancellationToken) {
        return await readService.GetSubscriptionsAsync(
            query.Page,
            query.Limit,
            query.Provider,
            query.Status,
            query.Search,
            query.FromUtc,
            query.ToUtc,
            cancellationToken).ConfigureAwait(false);
    }
}
