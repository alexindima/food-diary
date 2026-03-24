using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Cycles.Requests;

namespace FoodDiary.Presentation.Api.Features.Cycles.Mappings;

public static class CycleHttpMappings {
    public static CreateCycleCommand ToCommand(this CreateCycleHttpRequest request, Guid userId) =>
        new(
            new UserId(userId),
            request.StartDate,
            request.AverageLength,
            request.LutealLength,
            request.Notes);

    public static UpsertCycleDayCommand ToCommand(this UpsertCycleDayHttpRequest request, Guid userId, Guid cycleId) =>
        new(
            new UserId(userId),
            new CycleId(cycleId),
            request.Date,
            request.IsPeriod,
            request.Symptoms,
            request.Notes);
}
