using FoodDiary.Mediator;
using FoodDiary.Results;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;

namespace FoodDiary.Modules.DailyAdvices.Contracts.Queries.GetDailyAdviceGroups;

public sealed record GetDailyAdviceGroupsQuery(int Page, int Limit) : IRequest<Result<IReadOnlyList<DailyAdviceGroupModel>>>;
