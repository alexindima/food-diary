using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Cycles.Contracts.Models;

namespace FoodDiary.Modules.Cycles.Contracts.Queries.GetCurrentCycle;

public record GetCurrentCycleQuery(Guid? UserId, DateOnly? CurrentDate = null)
    : IQuery<Result<CycleModel?>>, IUserRequest;
