using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminUserLoginSummary;

public sealed class GetAdminUserLoginSummaryQueryHandler(IAdminUserLoginReadService readService)
    : IQueryHandler<GetAdminUserLoginSummaryQuery, Result<IReadOnlyList<AdminUserLoginDeviceSummaryModel>>> {
    public async Task<Result<IReadOnlyList<AdminUserLoginDeviceSummaryModel>>> Handle(
        GetAdminUserLoginSummaryQuery query,
        CancellationToken cancellationToken) {
        return await readService.GetSummaryAsync(query.FromUtc, query.ToUtc, cancellationToken).ConfigureAwait(false);
    }
}
