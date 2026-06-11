using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Commands.UpsertCycleFactor;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Cycles.Queries.GetCycleNutritionSummary;
using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
using FoodDiary.Presentation.Api.Features.Cycles.Requests;

namespace FoodDiary.Presentation.Api.Features.Cycles.Mappings;

public static class CycleHttpMappings {
    public static GetCurrentCycleQuery ToCurrentQuery(this Guid userId) => new(userId);

    public static GetCycleNutritionSummaryQuery ToNutritionSummaryQuery(this Guid userId, DateTime dateFrom, DateTime dateTo) =>
        new(userId, dateFrom, dateTo);

    public static CreateCycleCommand ToCommand(this CreateCycleHttpRequest request, Guid userId) =>
        new(
            userId,
            request.TrackingStartDate,
            request.Mode,
            request.AverageCycleLength,
            request.AveragePeriodLength,
            request.LutealLength,
            request.IsRegular,
            request.IsOnboardingComplete,
            request.ShowFertilityEstimates,
            request.DiscreetNotifications,
            request.Notes);

    public static UpsertCycleDayCommand ToCommand(this UpsertCycleDayHttpRequest request, Guid userId, Guid cycleProfileId) =>
        new(
            userId,
            cycleProfileId,
            request.Date,
            request.Bleeding?.ToCommandModel(),
            request.Symptoms.Select(static symptom => symptom.ToCommandModel()).ToList(),
            request.FertilitySignal?.ToCommandModel());

    public static UpsertCycleFactorCommand ToCommand(this UpsertCycleFactorHttpRequest request, Guid userId, Guid cycleProfileId) =>
        new(
            userId,
            cycleProfileId,
            request.Type,
            request.StartDate,
            request.EndDate,
            request.Notes,
            request.ClearNotes);

    private static BleedingLogCommandModel ToCommandModel(this BleedingLogHttpModel model) =>
        new(
            model.Type,
            model.Flow,
            model.PainImpact,
            model.Notes,
            model.ClearNotes);

    private static SymptomLogCommandModel ToCommandModel(this SymptomLogHttpModel model) =>
        new(
            model.Category,
            model.Intensity,
            model.Tags,
            model.Note,
            model.ClearNote);

    private static FertilitySignalCommandModel ToCommandModel(this FertilitySignalHttpModel model) =>
        new(
            model.BasalBodyTemperatureCelsius,
            model.OvulationTestResult,
            model.CervicalFluid,
            model.HadSex,
            model.Notes,
            model.ClearNotes);
}
