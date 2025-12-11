using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Hydration;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;

public record GetHydrationDailyTotalQuery(
    UserId? UserId,
    DateTime DateUtc) : IQuery<Result<HydrationDailyResponse>>;
