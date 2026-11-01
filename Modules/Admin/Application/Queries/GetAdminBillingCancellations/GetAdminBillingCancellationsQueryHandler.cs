using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Modules.Admin.Application.Services;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminBillingCancellations;

public sealed class GetAdminBillingCancellationsQueryHandler(IAdminBillingQuery billing)
    : IQueryHandler<GetAdminBillingCancellationsQuery, Result<AdminBillingCancellationReportReadModel>> {
    public async Task<Result<AdminBillingCancellationReportReadModel>> Handle(GetAdminBillingCancellationsQuery request, CancellationToken cancellationToken) {
        AdminBillingListFilter filter = AdminBillingQueryFilters.Create(request.Page, request.Limit, request.Provider,
            request.Status, kind: null, request.Search, request.FromUtc, request.ToUtc);
        return Result.Success(await billing.GetCancellationsAsync(filter, cancellationToken));
    }
}
