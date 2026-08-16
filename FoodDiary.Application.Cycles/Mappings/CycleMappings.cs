using FoodDiary.Application.Abstractions.Cycles.Models;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Cycles.Mappings;

public static class CycleMappings {
    public static CycleModel ToModel(this CycleProfileReadModel profile, CyclePredictionsModel? predictions = null) =>
        new(
            profile.Id,
            profile.UserId,
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
            predictions,
            BuildEpisodeModels(profile));

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
            predictions,
            profile.MenstrualEpisodes.OrderBy(episode => episode.StartDate).Select(episode => episode.ToModel()).ToList());

    private static IReadOnlyCollection<MenstrualEpisodeModel> BuildEpisodeModels(CycleProfileReadModel profile) {
        var persisted = new List<MenstrualEpisodeModel>(
            (profile.MenstrualEpisodes ?? []).Select(episode => episode.ToModel()));
        DateTime[] bleedingDates = [.. profile.BleedingEntries
            .Where(entry => entry.Type == global::FoodDiary.Domain.Enums.BleedingType.Bleeding)
            .Select(entry => CycleProfile.NormalizeDate(entry.Date))
            .Distinct()
            .Order()];

        for (int index = 0; index < bleedingDates.Length;) {
            DateTime start = bleedingDates[index];
            DateTime end = start;
            while (++index < bleedingDates.Length && (bleedingDates[index] - end).Days <= 2) {
                end = bleedingDates[index];
            }

            if (!persisted.Any(episode => episode.Status == global::FoodDiary.Domain.Enums.MenstrualEpisodeStatus.Confirmed &&
                episode.StartDate >= start.AddDays(-2) && episode.StartDate <= end.AddDays(2))) {
                persisted.Add(new MenstrualEpisodeModel(
                    Guid.Empty,
                    profile.Id,
                    start,
                    end,
                    global::FoodDiary.Domain.Enums.MenstrualEpisodeStatus.Inferred,
                    ExcludedFromPredictions: false));
            }
        }

        return persisted.OrderBy(episode => episode.StartDate).ToList();
    }

    public static MenstrualEpisodeModel ToModel(this MenstrualEpisodeReadModel episode) =>
        new(episode.Id, episode.CycleProfileId, episode.StartDate, episode.EndDate, episode.Status, episode.ExcludedFromPredictions);

    public static MenstrualEpisodeModel ToModel(this MenstrualEpisode episode) =>
        new(episode.Id.Value, episode.CycleProfileId.Value, episode.StartDate, episode.EndDate, episode.Status, episode.ExcludedFromPredictions);

    public static BleedingEntryModel ToModel(this BleedingEntryReadModel entry) =>
        new(
            entry.Id,
            entry.CycleProfileId,
            entry.Date,
            entry.Type,
            entry.Flow,
            entry.PainImpact,
            entry.Notes);

    public static BleedingEntryModel ToModel(this BleedingEntry entry) =>
        new(
            entry.Id.Value,
            entry.CycleProfileId.Value,
            entry.Date,
            entry.Type,
            entry.Flow,
            entry.PainImpact,
            entry.Notes);

    public static CycleSymptomEntryModel ToModel(this CycleSymptomEntryReadModel entry) =>
        new(
            entry.Id,
            entry.CycleProfileId,
            entry.Date,
            entry.Category,
            entry.Intensity,
            entry.Tags,
            entry.Note);

    public static CycleSymptomEntryModel ToModel(this CycleSymptomEntry entry) =>
        new(
            entry.Id.Value,
            entry.CycleProfileId.Value,
            entry.Date,
            entry.Category,
            entry.Intensity,
            entry.Tags,
            entry.Note);

    public static CycleFactorModel ToModel(this CycleFactorReadModel factor) =>
        new(
            factor.Id,
            factor.CycleProfileId,
            factor.Type,
            factor.StartDate,
            factor.EndDate,
            factor.Notes);

    public static CycleFactorModel ToModel(this CycleFactor factor) =>
        new(
            factor.Id.Value,
            factor.CycleProfileId.Value,
            factor.Type,
            factor.StartDate,
            factor.EndDate,
            factor.Notes);

    public static FertilitySignalModel ToModel(this FertilitySignalReadModel signal) =>
        new(
            signal.Id,
            signal.CycleProfileId,
            signal.Date,
            signal.BasalBodyTemperatureCelsius,
            signal.OvulationTestResult,
            signal.CervicalFluid,
            signal.HadSex,
            signal.Notes);

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
