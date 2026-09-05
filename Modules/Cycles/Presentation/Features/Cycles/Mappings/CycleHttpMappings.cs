using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Commands.DeleteMenstrualEpisode;
using FoodDiary.Application.Cycles.Commands.DeleteCycleProfile;
using FoodDiary.Application.Cycles.Commands.ConfirmPeriodStart;
using FoodDiary.Application.Cycles.Commands.ClearCycleDay;
using FoodDiary.Application.Cycles.Commands.UpsertCycleFactor;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Cycles.Commands.UpdateMenstrualEpisode;
using FoodDiary.Application.Cycles.Commands.UpdateCycleSettings;
using FoodDiary.Application.Cycles.Commands.UpdateCycleConsent;
using FoodDiary.Application.Cycles.Queries.GetCycleNutritionSummary;
using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
using FoodDiary.Presentation.Api.Features.Cycles.Requests;

namespace FoodDiary.Presentation.Api.Features.Cycles.Mappings;

public static class CycleHttpMappings {
    extension(Guid userId) {
        public GetCurrentCycleQuery ToCurrentQuery() => new(userId);

        public GetCycleNutritionSummaryQuery ToNutritionSummaryQuery(DateTime dateFrom, DateTime dateTo) =>
                new(userId, DateOnly.FromDateTime(dateFrom), DateOnly.FromDateTime(dateTo));
    }

    extension(CreateCycleHttpRequest request) {
        public CreateCycleCommand ToCommand(Guid userId) =>
                new(
                    userId,
                    DateOnly.FromDateTime(request.TrackingStartDate),
                    request.Mode,
                    request.AverageCycleLength,
                    request.AveragePeriodLength,
                    request.LutealLength,
                    request.IsRegular,
                    request.IsOnboardingComplete,
                    request.ShowFertilityEstimates,
                    request.DiscreetNotifications,
                    request.Notes,
                    request.Goal,
                    request.ReproductiveState,
                    request.HideFromDashboard,
                    request.CycleTrackingConsentGranted,
                    request.NutritionInsightsConsentGranted,
                    request.FertilitySignalsConsentGranted);
    }

    extension(UpdateCycleSettingsHttpRequest request) {
        public UpdateCycleSettingsCommand ToCommand(Guid userId, Guid cycleProfileId) =>
            new(
                userId,
                cycleProfileId,
                request.Mode,
                request.AverageCycleLength,
                request.AveragePeriodLength,
                request.LutealLength,
                request.IsRegular,
                request.ShowFertilityEstimates,
                request.DiscreetNotifications,
                request.Goal,
                request.ReproductiveState,
                request.HideFromDashboard);

    }

    extension(UpdateCycleConsentHttpRequest request) {
        public UpdateCycleConsentCommand ToCommand(Guid userId, Guid cycleProfileId, int purpose) =>
            new(userId, cycleProfileId, purpose, request.Granted);
    }

    extension(UpsertCycleDayHttpRequest request) {
        public UpsertCycleDayCommand ToCommand(Guid userId, Guid cycleProfileId) =>
                new(
                    userId,
                    cycleProfileId,
                    DateOnly.FromDateTime(request.Date),
                    request.Bleeding?.ToCommandModel(),
                    request.Symptoms.Select(static symptom => symptom.ToCommandModel()).ToList(),
                    request.FertilitySignal?.ToCommandModel(),
                    request.ClearBleeding,
                    request.ClearSymptomCategories ?? [],
                    request.ClearFertilitySignal);
    }

    extension(Guid cycleProfileId) {
        public DeleteCycleProfileCommand ToDeleteCommand(Guid userId) => new(userId, cycleProfileId);

        public ClearCycleDayCommand ToClearDayCommand(Guid userId, DateTime date) =>
                new(userId, cycleProfileId, DateOnly.FromDateTime(date));

        public DeleteMenstrualEpisodeCommand ToDeleteMenstrualEpisodeCommand(Guid userId, Guid menstrualEpisodeId) =>
            new(userId, cycleProfileId, menstrualEpisodeId);
    }

    extension(UpsertCycleFactorHttpRequest request) {
        public UpsertCycleFactorCommand ToCommand(Guid userId, Guid cycleProfileId) =>
                new(
                    userId,
                    cycleProfileId,
                    request.Type,
                    DateOnly.FromDateTime(request.StartDate),
                    request.EndDate.HasValue ? DateOnly.FromDateTime(request.EndDate.Value) : null,
                    request.Notes,
                    request.ClearNotes);
    }

    extension(ConfirmPeriodStartHttpRequest request) {
        public ConfirmPeriodStartCommand ToCommand(Guid userId, Guid cycleProfileId) =>
            new(userId, cycleProfileId, DateOnly.FromDateTime(request.Date));
    }

    extension(UpdateMenstrualEpisodeHttpRequest request) {
        public UpdateMenstrualEpisodeCommand ToCommand(Guid userId, Guid cycleProfileId, Guid menstrualEpisodeId) =>
            new(
                userId,
                cycleProfileId,
                menstrualEpisodeId,
                DateOnly.FromDateTime(request.StartDate),
                request.EndDate.HasValue ? DateOnly.FromDateTime(request.EndDate.Value) : null,
                request.ExcludedFromPredictions);
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
