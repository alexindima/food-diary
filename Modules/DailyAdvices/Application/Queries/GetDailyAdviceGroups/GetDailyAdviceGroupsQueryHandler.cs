using FoodDiary.Mediator;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Models;
using FoodDiary.Results;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Modules.DailyAdvices.Contracts.Queries.GetDailyAdviceGroups;

namespace FoodDiary.Modules.DailyAdvices.Application.Queries.GetDailyAdviceGroups;

public sealed class GetDailyAdviceGroupsQueryHandler(IDailyAdviceReadModelRepository repository)
    : IRequestHandler<GetDailyAdviceGroupsQuery, Result<IReadOnlyList<DailyAdviceGroupModel>>> {
    public async Task<Result<IReadOnlyList<DailyAdviceGroupModel>>> Handle(GetDailyAdviceGroupsQuery request, CancellationToken cancellationToken) {
        IReadOnlyList<DailyAdviceReadModel> items = await repository.GetAllReadModelsAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<DailyAdviceGroupModel>>(items
            .GroupBy(item => item.GroupId == Guid.Empty ? item.Id : item.GroupId)
            .Select(group => new DailyAdviceGroupModel(group.Key,
                group.SingleOrDefault(item => string.Equals(item.Locale, "ru", StringComparison.Ordinal))?.Value,
                group.SingleOrDefault(item => string.Equals(item.Locale, "en", StringComparison.Ordinal))?.Value,
                group.First().Weight, group.First().Tag))
            .OrderBy(item => item.Ru ?? item.En, StringComparer.Ordinal).ThenBy(item => item.Id).ToArray());
    }
}
