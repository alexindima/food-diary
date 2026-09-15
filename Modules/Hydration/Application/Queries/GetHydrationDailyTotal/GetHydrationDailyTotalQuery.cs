using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Hydration.Contracts.Models;

namespace FoodDiary.Modules.Hydration.Application.Queries.GetHydrationDailyTotal;

public record GetHydrationDailyTotalQuery(
    Guid? UserId,
    DateTime DateUtc) : IQuery<Result<HydrationDailyModel>>, IUserRequest;
