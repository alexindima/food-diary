using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Cycles.Models;
using FoodDiary.Presentation.Api.Features.Cycles.Requests;

namespace FoodDiary.Presentation.Api.Features.Cycles.Mappings;

public static class CycleHttpMappings {
    public static GetCurrentCycleQuery ToCurrentQuery(this UserId userId) => new(userId);

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
            request.Symptoms.ToModel(),
            request.Notes);

    private static DailySymptomsModel ToModel(this DailySymptomsHttpModel model) =>
        new(
            model.Pain,
            model.Mood,
            model.Edema,
            model.Headache,
            model.Energy,
            model.SleepQuality,
            model.Libido
        );
}
