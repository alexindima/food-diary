using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;

namespace FoodDiary.Modules.DailyAdvices.Contracts.Queries.GetDailyAdvicesForAdministration;

public sealed record GetDailyAdvicesForAdministrationQuery : IQuery<Result<IReadOnlyList<DailyAdviceModel>>>;
