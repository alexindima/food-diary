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
                ToHttpDate(model.TrackingStartDate),
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
                ToHttpResponse(model.Predictions),
                (int)model.Goal,
                (int)model.ReproductiveState,
                model.HideFromDashboard,
                ToHttpResponse(model.Consents),
                ToHttpResponse(model.PredictionRevisions)
            );
        }
    }

    extension(CycleLogDayModel model) {
        public CycleLogDayHttpResponse ToHttpResponse() =>
                new(
                    model.CycleProfileId,
                    ToHttpDate(model.Date),
                    model.BleedingEntries.ToHttpResponseList(ToHttpResponse),
                    model.Symptoms.ToHttpResponseList(ToHttpResponse),
                    model.FertilitySignal?.ToHttpResponse());
    }

    extension(CycleNutritionSummaryModel model) {
        public CycleNutritionSummaryHttpResponse ToHttpResponse() =>
                new(
                    ToHttpDate(model.DateFrom),
                    ToHttpDate(model.DateTo),
                    model.LoggedCycleDays,
                    model.DaysWithMeals,
                    model.BleedingDays,
                    model.AverageCaloriesOnBleedingDays,
                    model.AverageCaloriesOnNonBleedingCycleDays,
                    model.AverageFiberOnBleedingDays,
                    model.AverageFiberOnNonBleedingCycleDays,
                    model.AveragePainImpactOnDaysWithMeals,
                    model.HasEnoughNutritionData,
                    model.ConsentRequired,
                    model.CompletedCyclesAnalyzed,
                    model.ComparableCycles,
                    model.DataSufficiency,
                    model.ReasonCodes,
                    model.AlgorithmVersion);
    }

    extension(BleedingEntryModel model) {
        public BleedingEntryHttpResponse ToHttpResponse() =>
                new(
                    model.Id,
                    model.CycleProfileId,
                    ToHttpDate(model.Date),
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
                    ToHttpDate(model.Date),
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
                    ToHttpDate(model.StartDate),
                    ToHttpDate(model.EndDate),
                    model.Notes);
    }

    extension(FertilitySignalModel model) {
        public FertilitySignalHttpResponse ToHttpResponse() =>
                new(
                    model.Id,
                    model.CycleProfileId,
                    ToHttpDate(model.Date),
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
                ToHttpDate(model.StartDate),
                ToHttpDate(model.EndDate),
                (int)model.Status,
                model.ExcludedFromPredictions);
    }

    private static DateTime ToHttpDate(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    private static DateTime? ToHttpDate(DateOnly? date) => date.HasValue ? ToHttpDate(date.Value) : null;

    private static CyclePredictionsHttpResponse? ToHttpResponse(CyclePredictionsModel? predictions) =>
        predictions is null
            ? null
            : new CyclePredictionsHttpResponse(
                ToHttpDate(predictions.NextPeriodStartFrom),
                ToHttpDate(predictions.NextPeriodStartTo),
                ToHttpDate(predictions.OvulationFrom),
                ToHttpDate(predictions.OvulationTo),
                ToHttpDate(predictions.PmsWindowStart),
                ToHttpDate(predictions.PmsWindowEnd),
                predictions.Confidence,
                predictions.Rationale,
                predictions.DataSufficiency,
                predictions.PatternConsistency,
                predictions.CompletedCycleCount,
                predictions.UsedEpisodeCount,
                predictions.ExcludedEpisodeCount,
                predictions.ReasonCodes,
                predictions.AlgorithmVersion,
                predictions.CalibrationSampleCount,
                predictions.HistoricalCoveragePercent,
                predictions.MeanAbsoluteErrorDays);

    private static IReadOnlyCollection<CycleConsentHttpResponse> ToHttpResponse(
        IReadOnlyCollection<CycleConsentModel>? consents) =>
        [.. (consents ?? []).Select(consent => new CycleConsentHttpResponse(
            consent.Id,
            (int)consent.Purpose,
            consent.GrantedAtUtc,
            consent.RevokedAtUtc,
            consent.IsActive))];

    private static IReadOnlyCollection<CyclePredictionRevisionHttpResponse> ToHttpResponse(
        IReadOnlyCollection<CyclePredictionRevisionModel>? revisions) =>
        [.. (revisions ?? []).Select(revision => new CyclePredictionRevisionHttpResponse(
            revision.Id,
            revision.GeneratedAtUtc,
            ToHttpDate(revision.NextPeriodStartFrom),
            ToHttpDate(revision.NextPeriodStartTo),
            revision.Confidence,
            revision.DataSufficiency,
            revision.PatternConsistency,
            revision.CompletedCycleCount,
            revision.CalibrationSampleCount,
            revision.HistoricalCoveragePercent,
            revision.MeanAbsoluteErrorDays,
            revision.ReasonCodes,
            revision.AlgorithmVersion))];
}
