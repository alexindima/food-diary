using FoodDiary.Application.Cycles.Models;
using FoodDiary.Presentation.Api.Features.Cycles.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Cycles.Mappings;

public static class CycleHttpResponseMappings {
    public static CycleHttpResponse ToHttpResponse(this CycleModel model) {
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
                    model.Predictions.Rationale)
        );
    }

    public static CycleLogDayHttpResponse ToHttpResponse(this CycleLogDayModel model) =>
        new(
            model.CycleProfileId,
            model.Date,
            model.BleedingEntries.ToHttpResponseList(ToHttpResponse),
            model.Symptoms.ToHttpResponseList(ToHttpResponse),
            model.FertilitySignal?.ToHttpResponse());

    public static BleedingEntryHttpResponse ToHttpResponse(this BleedingEntryModel model) =>
        new(
            model.Id,
            model.CycleProfileId,
            model.Date,
            (int)model.Type,
            (int)model.Flow,
            model.PainImpact,
            model.Notes);

    public static CycleSymptomEntryHttpResponse ToHttpResponse(this CycleSymptomEntryModel model) =>
        new(
            model.Id,
            model.CycleProfileId,
            model.Date,
            (int)model.Category,
            model.Intensity,
            model.Tags,
            model.Note);

    public static CycleFactorHttpResponse ToHttpResponse(this CycleFactorModel model) =>
        new(
            model.Id,
            model.CycleProfileId,
            (int)model.Type,
            model.StartDate,
            model.EndDate,
            model.Notes);

    public static FertilitySignalHttpResponse ToHttpResponse(this FertilitySignalModel model) =>
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
