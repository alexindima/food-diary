using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Admin.Services;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminBillingPayments;

public sealed class GetAdminBillingPaymentsQueryHandler(IAdminBillingReadRepository billingRepository)
    : IQueryHandler<GetAdminBillingPaymentsQuery, Result<PagedResponse<AdminBillingPaymentReadModel>>> {
    public async Task<Result<PagedResponse<AdminBillingPaymentReadModel>>> Handle(GetAdminBillingPaymentsQuery query, CancellationToken cancellationToken) {
        AdminBillingListFilter filter = AdminBillingQueryFilters.Create(
            query.Page,
            query.Limit,
            query.Provider,
            query.Status,
            query.Kind,
            query.Search,
            query.FromUtc,
            query.ToUtc);
        (IReadOnlyList<AdminBillingPaymentReadModel> items, int totalItems) =
            await billingRepository.GetPaymentsAsync(filter, cancellationToken).ConfigureAwait(false);

        return Result.Success(AdminBillingQueryFilters.ToPagedResponse(items, filter.Page, filter.Limit, totalItems));
    }
}
