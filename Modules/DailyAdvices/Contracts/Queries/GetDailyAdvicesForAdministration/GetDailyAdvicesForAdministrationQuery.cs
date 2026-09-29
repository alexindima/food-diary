using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;

namespace FoodDiary.Modules.DailyAdvices.Contracts.Queries.GetDailyAdvicesForAdministration;

public sealed record GetDailyAdvicesForAdministrationQuery(int Page, int Limit) : IQuery<Result<IReadOnlyList<DailyAdviceModel>>>;
