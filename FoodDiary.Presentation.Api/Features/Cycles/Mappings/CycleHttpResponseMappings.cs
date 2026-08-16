using FoodDiary.Application.Cycles.Models;
using FoodDiary.Presentation.Api.Features.Cycles.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Cycles.Mappings;

public static class CycleHttpResponseMappings {
    extension(CycleModel model) {
        public CycleHttpResponse ToHttpResponse() {
            return new CycleHttpResponse(
                model.Id,
                model.UserId,
                (int)model.Mode,
                (int)model.Confidence,
                model.TrackingStartDate,
                model.AverageCycleLength,
                model.AveragePeriodLength,
                model.LutealLength,
                model.IsRegular,
                model.IsOnboardingComplete,
                model.ShowFertilityEstimates,
                model.DiscreetNotifications,
                model.Notes,
                model.BleedingEntries.ToHttpResponseList(ToHttpResponse),
                model.Symptoms.ToHttpResponseList(ToHttpResponse),
                model.Factors.ToHttpResponseList(ToHttpResponse),
                model.FertilitySignals.ToHttpResponseList(ToHttpResponse),
                (model.MenstrualEpisodes ?? []).ToHttpResponseList(ToHttpResponse),
                model.Predictions is null
                    ? null
                    : new CyclePredictionsHttpResponse(
                        model.Predictions.NextPeriodStartFrom,
                        model.Predictions.NextPeriodStartTo,
                        model.Predictions.OvulationFrom,
                        model.Predictions.OvulationTo,
                        model.Predictions.PmsWindowStart,
                        model.Predictions.PmsWindowEnd,
                        model.Predictions.Confidence,
                        model.Predictions.Rationale,
                        model.Predictions.DataSufficiency,
                        model.Predictions.PatternConsistency,
                        model.Predictions.CompletedCycleCount,
                        model.Predictions.ReasonCodes,
                        model.Predictions.AlgorithmVersion)
            );
        }
    }

    extension(CycleLogDayModel model) {
        public CycleLogDayHttpResponse ToHttpResponse() =>
                new(
                    model.CycleProfileId,
                    model.Date,
                    model.BleedingEntries.ToHttpResponseList(ToHttpResponse),
                    model.Symptoms.ToHttpResponseList(ToHttpResponse),
                    model.FertilitySignal?.ToHttpResponse());
    }

    extension(CycleNutritionSummaryModel model) {
        public CycleNutritionSummaryHttpResponse ToHttpResponse() =>
                new(
                    model.DateFrom,
                    model.DateTo,
                    model.LoggedCycleDays,
                    model.DaysWithMeals,
                    model.BleedingDays,
                    model.AverageCaloriesOnBleedingDays,
                    model.AverageCaloriesOnNonBleedingCycleDays,
                    model.AverageFiberOnBleedingDays,
                    model.AverageFiberOnNonBleedingCycleDays,
                    model.AveragePainImpactOnDaysWithMeals,
                    model.HasEnoughNutritionData);
    }

    extension(BleedingEntryModel model) {
        public BleedingEntryHttpResponse ToHttpResponse() =>
                new(
                    model.Id,
                    model.CycleProfileId,
                    model.Date,
                    (int)model.Type,
                    (int)model.Flow,
                    model.PainImpact,
                    model.Notes);
    }

    extension(CycleSymptomEntryModel model) {
        public CycleSymptomEntryHttpResponse ToHttpResponse() =>
                new(
                    model.Id,
                    model.CycleProfileId,
                    model.Date,
                    (int)model.Category,
                    model.Intensity,
                    model.Tags,
                    model.Note);
    }

    extension(CycleFactorModel model) {
        public CycleFactorHttpResponse ToHttpResponse() =>
                new(
                    model.Id,
                    model.CycleProfileId,
                    (int)model.Type,
                    model.StartDate,
                    model.EndDate,
                    model.Notes);
    }

    extension(FertilitySignalModel model) {
        public FertilitySignalHttpResponse ToHttpResponse() =>
                new(
                    model.Id,
                    model.CycleProfileId,
                    model.Date,
                    model.BasalBodyTemperatureCelsius,
                    model.OvulationTestResult.HasValue ? (int)model.OvulationTestResult.Value : null,
                    model.CervicalFluid,
                    model.HadSex,
                    model.Notes);
    }

    extension(MenstrualEpisodeModel model) {
        public MenstrualEpisodeHttpResponse ToHttpResponse() =>
            new(
                model.Id,
                model.CycleProfileId,
                model.StartDate,
                model.EndDate,
                (int)model.Status,
                model.ExcludedFromPredictions);
    }
}
