using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Admin.Queries.GetAdminUserLoginSummary;

public sealed class GetAdminUserLoginSummaryQueryHandler(IUserLoginEventRepository repository)
    : IQueryHandler<GetAdminUserLoginSummaryQuery, Result<IReadOnlyList<AdminUserLoginDeviceSummaryModel>>> {
    public async Task<Result<IReadOnlyList<AdminUserLoginDeviceSummaryModel>>> Handle(
        GetAdminUserLoginSummaryQuery query,
        CancellationToken cancellationToken) {
        var summary = await repository.GetDeviceSummaryAsync(query.FromUtc, query.ToUtc, cancellationToken);
        return Result.Success<IReadOnlyList<AdminUserLoginDeviceSummaryModel>>(summary
            .Select(item => new AdminUserLoginDeviceSummaryModel(item.Key, item.Count, item.LastSeenAtUtc))
            .ToArray());
    }
}
