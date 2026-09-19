using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Mediator;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Modules.DailyAdvices.Contracts.Queries.GetDailyAdviceGroups;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminDailyAdviceGroups;

public sealed class GetAdminDailyAdviceGroupsQueryHandler(ISender sender)
    : IQueryHandler<GetAdminDailyAdviceGroupsQuery, Result<IReadOnlyList<AdminDailyAdviceGroupModel>>> {
    public async Task<Result<IReadOnlyList<AdminDailyAdviceGroupModel>>> Handle(GetAdminDailyAdviceGroupsQuery query, CancellationToken cancellationToken) {
        Result<IReadOnlyList<DailyAdviceGroupModel>> result = await sender.Send(new GetDailyAdviceGroupsQuery(), cancellationToken).ConfigureAwait(false);
        return result.IsFailure ? Result.Failure<IReadOnlyList<AdminDailyAdviceGroupModel>>(result.Error)
            : Result.Success<IReadOnlyList<AdminDailyAdviceGroupModel>>(result.Value.Select(item =>
                new AdminDailyAdviceGroupModel(item.Id, item.Ru, item.En, item.Weight, item.Tag)).ToArray());
    }
}
