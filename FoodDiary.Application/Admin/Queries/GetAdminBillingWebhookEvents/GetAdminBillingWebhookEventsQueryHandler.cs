using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Results;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminBillingWebhookEvents;

public sealed class GetAdminBillingWebhookEventsQueryHandler(IAdminBillingReadService readService)
    : IQueryHandler<GetAdminBillingWebhookEventsQuery, Result<PagedResponse<AdminBillingWebhookEventReadModel>>> {
    public async Task<Result<PagedResponse<AdminBillingWebhookEventReadModel>>> Handle(
        GetAdminBillingWebhookEventsQuery query,
        CancellationToken cancellationToken) {
        return await readService.GetWebhookEventsAsync(
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
