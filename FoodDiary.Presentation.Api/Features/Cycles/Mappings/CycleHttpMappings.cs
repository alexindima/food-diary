using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Commands.ClearCycleDay;
using FoodDiary.Application.Cycles.Commands.UpsertCycleFactor;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Cycles.Queries.GetCycleNutritionSummary;
using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
using FoodDiary.Presentation.Api.Features.Cycles.Requests;

namespace FoodDiary.Presentation.Api.Features.Cycles.Mappings;

public static class CycleHttpMappings {
    extension(Guid userId) {
        public GetCurrentCycleQuery ToCurrentQuery() => new(userId);

        public GetCycleNutritionSummaryQuery ToNutritionSummaryQuery(DateTime dateFrom, DateTime dateTo) =>
                new(userId, dateFrom, dateTo);
    }

    extension(CreateCycleHttpRequest request) {
        public CreateCycleCommand ToCommand(Guid userId) =>
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
    }

    extension(UpsertCycleDayHttpRequest request) {
        public UpsertCycleDayCommand ToCommand(Guid userId, Guid cycleProfileId) =>
                new(
                    userId,
                    cycleProfileId,
                    request.Date,
                    request.Bleeding?.ToCommandModel(),
                    request.Symptoms.Select(static symptom => symptom.ToCommandModel()).ToList(),
                    request.FertilitySignal?.ToCommandModel());
    }

    extension(Guid cycleProfileId) {
        public ClearCycleDayCommand ToClearDayCommand(Guid userId, DateTime date) =>
                new(userId, cycleProfileId, date);
    }

    extension(UpsertCycleFactorHttpRequest request) {
        public UpsertCycleFactorCommand ToCommand(Guid userId, Guid cycleProfileId) =>
                new(
                    userId,
                    cycleProfileId,
                    request.Type,
                    request.StartDate,
                    request.EndDate,
                    request.Notes,
                    request.ClearNotes);
    }

    extension(BleedingLogHttpModel model) {
        private BleedingLogCommandModel ToCommandModel() =>
                new(
                    model.Type,
                    model.Flow,
                    model.PainImpact,
                    model.Notes,
                    model.ClearNotes);
    }

    extension(SymptomLogHttpModel model) {
        private SymptomLogCommandModel ToCommandModel() =>
                new(
                    model.Category,
                    model.Intensity,
                    model.Tags,
                    model.Note,
                    model.ClearNote);
    }

    extension(FertilitySignalHttpModel model) {
        private FertilitySignalCommandModel ToCommandModel() =>
                new(
                    model.BasalBodyTemperatureCelsius,
                    model.OvulationTestResult,
                    model.CervicalFluid,
                    model.HadSex,
                    model.Notes,
                    model.ClearNotes);
    }
}
