using FoodDiary.Application.Abstractions.Authentication.Queries.GetLoginDeviceSummary;
using FoodDiary.Mediator;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminUserLoginSummary;

public sealed class GetAdminUserLoginSummaryQueryHandler(ISender sender)
    : IQueryHandler<GetAdminUserLoginSummaryQuery, Result<IReadOnlyList<AdminUserLoginDeviceSummaryModel>>> {
    public async Task<Result<IReadOnlyList<AdminUserLoginDeviceSummaryModel>>> Handle(GetAdminUserLoginSummaryQuery query, CancellationToken cancellationToken) {
        IReadOnlyList<UserLoginDeviceSummaryModel> summary =
            await sender.Send(new GetLoginDeviceSummaryQuery(query.FromUtc, query.ToUtc), cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<AdminUserLoginDeviceSummaryModel>>(summary
            .Select(static item => new AdminUserLoginDeviceSummaryModel(item.Key, item.Count, item.LastSeenAtUtc))
            .ToArray());
    }
}
