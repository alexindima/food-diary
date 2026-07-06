using FoodDiary.Application.Abstractions.Cycles.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Cycles.Common;

public interface ICycleReadRepository {
    Task<CycleProfile?> GetByIdAsync(
        CycleProfileId id,
        UserId userId,
        bool includeDetails = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<CycleProfile?> GetCurrentAsync(
        UserId userId,
        bool includeDetails = false,
        CancellationToken cancellationToken = default);

    async Task<CycleProfileReadModel?> GetCurrentReadModelAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        CycleProfile? profile = await GetCurrentAsync(userId, includeDetails: true, cancellationToken).ConfigureAwait(false);
        return profile is null ? null : ToReadModel(profile);
    }

    Task<IReadOnlyList<CycleProfile>> GetByUserAsync(
        UserId userId,
        bool includeDetails = false,
        CancellationToken cancellationToken = default);

    private static CycleProfileReadModel ToReadModel(CycleProfile profile) =>
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
            [.. profile.BleedingEntries.Select(static entry => new BleedingEntryReadModel(
                entry.Id.Value,
                entry.CycleProfileId.Value,
                entry.Date,
                entry.Type,
                entry.Flow,
                entry.PainImpact,
                entry.Notes))],
            [.. profile.SymptomEntries.Select(static entry => new CycleSymptomEntryReadModel(
                entry.Id.Value,
                entry.CycleProfileId.Value,
                entry.Date,
                entry.Category,
                entry.Intensity,
                entry.Tags,
                entry.Note))],
            [.. profile.Factors.Select(static factor => new CycleFactorReadModel(
                factor.Id.Value,
                factor.CycleProfileId.Value,
                factor.Type,
                factor.StartDate,
                factor.EndDate,
                factor.Notes))],
            [.. profile.FertilitySignals.Select(static signal => new FertilitySignalReadModel(
                signal.Id.Value,
                signal.CycleProfileId.Value,
                signal.Date,
                signal.BasalBodyTemperatureCelsius,
                signal.OvulationTestResult,
                signal.CervicalFluid,
                signal.HadSex,
                signal.Notes))]);
}
