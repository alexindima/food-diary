using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminBillingPayments;

public sealed class GetAdminBillingPaymentsQueryHandler(IAdminBillingRepository billingRepository)
    : IQueryHandler<GetAdminBillingPaymentsQuery, Result<PagedResponse<AdminBillingPaymentReadModel>>> {
    public async Task<Result<PagedResponse<AdminBillingPaymentReadModel>>> Handle(
        GetAdminBillingPaymentsQuery query,
        CancellationToken cancellationToken) {
        AdminBillingListFilter filter = AdminBillingQueryFilters.Create(
            query.Page,
            query.Limit,
            query.Provider,
            query.Status,
            query.Kind,
            query.Search,
            query.FromUtc,
            query.ToUtc);
        (IReadOnlyList<AdminBillingPaymentReadModel> Items, int TotalItems) pageData = await billingRepository.GetPaymentsAsync(filter, cancellationToken).ConfigureAwait(false);
        int totalPages = (int)Math.Ceiling(pageData.TotalItems / (double)filter.Limit);
        return Result.Success(new PagedResponse<AdminBillingPaymentReadModel>(
            pageData.Items,
            filter.Page,
            filter.Limit,
            totalPages,
            pageData.TotalItems));
    }
}
