using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Cycles.Models;

namespace FoodDiary.Application.Cycles.Queries.GetCurrentCycle;

public record GetCurrentCycleQuery(Guid? UserId)
    : IQuery<Result<CycleModel?>>, IUserRequest;
