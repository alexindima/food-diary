using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Hydration.Models;

namespace FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;

public record GetHydrationDailyTotalQuery(
    Guid? UserId,
    DateTime DateUtc) : IQuery<Result<HydrationDailyModel>>;
