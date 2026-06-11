using FoodDiary.Application.Cycles.Models;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Cycles.Mappings;

public static class CycleMappings {
    public static CycleModel ToModel(this CycleProfile profile, CyclePredictionsModel? predictions = null) =>
        new(
            profile.Id.Value,
            profile.UserId.Value,
            profile.Mode,
            profile.Confidence,
            profile.TrackingStartDate,
            profile.AverageCycleLength,
            profile.AveragePeriodLength,
            profile.LutealLength,
            profile.IsRegular,
            profile.IsOnboardingComplete,
            profile.ShowFertilityEstimates,
            profile.DiscreetNotifications,
            profile.Notes,
            profile.BleedingEntries.OrderBy(entry => entry.Date).ThenBy(entry => entry.Type).Select(entry => entry.ToModel()).ToList(),
            profile.SymptomEntries.OrderBy(entry => entry.Date).ThenBy(entry => entry.Category).Select(entry => entry.ToModel()).ToList(),
            profile.Factors.OrderBy(factor => factor.StartDate).ThenBy(factor => factor.Type).Select(factor => factor.ToModel()).ToList(),
            profile.FertilitySignals.OrderBy(signal => signal.Date).Select(signal => signal.ToModel()).ToList(),
            predictions);

    public static BleedingEntryModel ToModel(this BleedingEntry entry) =>
        new(
            entry.Id.Value,
            entry.CycleProfileId.Value,
            entry.Date,
            entry.Type,
            entry.Flow,
            entry.PainImpact,
            entry.Notes);

    public static CycleSymptomEntryModel ToModel(this CycleSymptomEntry entry) =>
        new(
            entry.Id.Value,
            entry.CycleProfileId.Value,
            entry.Date,
            entry.Category,
            entry.Intensity,
            entry.Tags,
            entry.Note);

    public static CycleFactorModel ToModel(this CycleFactor factor) =>
        new(
            factor.Id.Value,
            factor.CycleProfileId.Value,
            factor.Type,
            factor.StartDate,
            factor.EndDate,
            factor.Notes);

    public static FertilitySignalModel ToModel(this FertilitySignal signal) =>
        new(
            signal.Id.Value,
            signal.CycleProfileId.Value,
            signal.Date,
            signal.BasalBodyTemperatureCelsius,
            signal.OvulationTestResult,
            signal.CervicalFluid,
            signal.HadSex,
            signal.Notes);

    public static CycleLogDayModel ToDayModel(this CycleProfile profile, DateTime date) {
        DateTime normalizedDate = CycleProfile.NormalizeDate(date);
        return new CycleLogDayModel(
            profile.Id.Value,
            normalizedDate,
            profile.BleedingEntries
                .Where(entry => entry.Date == normalizedDate)
                .OrderBy(entry => entry.Type)
                .Select(entry => entry.ToModel())
                .ToList(),
            profile.SymptomEntries
                .Where(entry => entry.Date == normalizedDate)
                .OrderBy(entry => entry.Category)
                .Select(entry => entry.ToModel())
                .ToList(),
            profile.FertilitySignals
                .Where(signal => signal.Date == normalizedDate)
                .Select(signal => signal.ToModel())
                .FirstOrDefault());
    }
}
