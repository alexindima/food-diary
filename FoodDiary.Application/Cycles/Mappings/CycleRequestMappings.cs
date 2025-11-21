using System;
using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Contracts.Cycles;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Cycles.Mappings;

public static class CycleRequestMappings
{
    public static CreateCycleCommand ToCommand(this CreateCycleRequest request, Guid? userId) =>
        new(
            userId.HasValue ? new UserId(userId.Value) : null,
            request.StartDate,
            request.AverageLength,
            request.LutealLength,
            request.Notes);

    public static UpsertCycleDayCommand ToCommand(this UpsertCycleDayRequest request, Guid? userId, Guid cycleId) =>
        new(
            userId.HasValue ? new UserId(userId.Value) : null,
            new CycleId(cycleId),
            request.Date,
            request.IsPeriod,
            request.Symptoms,
            request.Notes);
}
