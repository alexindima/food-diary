using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminBillingWebhookEvents;

public sealed class GetAdminBillingWebhookEventsQueryHandler(IAdminBillingRepository billingRepository)
    : IQueryHandler<GetAdminBillingWebhookEventsQuery, Result<PagedResponse<AdminBillingWebhookEventReadModel>>> {
    public async Task<Result<PagedResponse<AdminBillingWebhookEventReadModel>>> Handle(
        GetAdminBillingWebhookEventsQuery query,
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
        var pageData = await billingRepository.GetWebhookEventsAsync(filter, cancellationToken);
        var totalPages = (int)Math.Ceiling(pageData.TotalItems / (double)filter.Limit);
        return Result.Success(new PagedResponse<AdminBillingWebhookEventReadModel>(
            pageData.Items,
            filter.Page,
            filter.Limit,
            totalPages,
            pageData.TotalItems));
    }
}
