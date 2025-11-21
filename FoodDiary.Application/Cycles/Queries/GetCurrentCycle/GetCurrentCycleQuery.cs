using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Cycles;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Cycles.Queries.GetCurrentCycle;

public record GetCurrentCycleQuery(UserId? UserId)
    : IQuery<Result<CycleResponse?>>;
