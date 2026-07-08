using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Results;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminBillingPayments;

public sealed class GetAdminBillingPaymentsQueryHandler(IAdminBillingReadService readService)
    : IQueryHandler<GetAdminBillingPaymentsQuery, Result<PagedResponse<AdminBillingPaymentReadModel>>> {
    public async Task<Result<PagedResponse<AdminBillingPaymentReadModel>>> Handle(
        GetAdminBillingPaymentsQuery query,
        CancellationToken cancellationToken) {
        return await readService.GetPaymentsAsync(
            query.Page,
            query.Limit,
            query.Provider,
            query.Status,
            query.Kind,
            query.Search,
            query.FromUtc,
            query.ToUtc,
            cancellationToken).ConfigureAwait(false);
    }
}
