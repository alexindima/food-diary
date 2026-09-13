using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminUserLoginSummary;

public sealed class GetAdminUserLoginSummaryQueryHandler(IAuthenticationLoginEventReadService readService)
    : IQueryHandler<GetAdminUserLoginSummaryQuery, Result<IReadOnlyList<AdminUserLoginDeviceSummaryModel>>> {
    public async Task<Result<IReadOnlyList<AdminUserLoginDeviceSummaryModel>>> Handle(GetAdminUserLoginSummaryQuery query, CancellationToken cancellationToken) {
        IReadOnlyList<UserLoginDeviceSummaryModel> summary =
            await readService.GetDeviceSummaryAsync(query.FromUtc, query.ToUtc, cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<AdminUserLoginDeviceSummaryModel>>(summary
            .Select(static item => new AdminUserLoginDeviceSummaryModel(item.Key, item.Count, item.LastSeenAtUtc))
            .ToArray());
    }
}
