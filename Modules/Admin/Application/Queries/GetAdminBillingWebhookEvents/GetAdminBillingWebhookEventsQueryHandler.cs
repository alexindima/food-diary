using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Admin.Services;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminBillingWebhookEvents;

public sealed class GetAdminBillingWebhookEventsQueryHandler(IAdminBillingReadRepository billingRepository)
    : IQueryHandler<GetAdminBillingWebhookEventsQuery, Result<PagedResponse<AdminBillingWebhookEventReadModel>>> {
    public async Task<Result<PagedResponse<AdminBillingWebhookEventReadModel>>> Handle(GetAdminBillingWebhookEventsQuery query, CancellationToken cancellationToken) {
        AdminBillingListFilter filter = AdminBillingQueryFilters.Create(
            query.Page,
            query.Limit,
            query.Provider,
            query.Status,
            kind: null,
            query.Search,
            query.FromUtc,
            query.ToUtc);
        (IReadOnlyList<AdminBillingWebhookEventReadModel> items, int totalItems) =
            await billingRepository.GetWebhookEventsAsync(filter, cancellationToken).ConfigureAwait(false);

        return Result.Success(AdminBillingQueryFilters.ToPagedResponse(items, filter.Page, filter.Limit, totalItems));
    }
}
