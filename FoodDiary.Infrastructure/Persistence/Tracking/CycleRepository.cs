using Microsoft.EntityFrameworkCore;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Abstractions.Cycles.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

public sealed class CycleRepository(FoodDiaryDbContext context) : ICycleRepository {
    public async Task<CycleProfile> AddAsync(CycleProfile profile, CancellationToken cancellationToken = default) {
        await context.CycleProfiles.AddAsync(profile, cancellationToken).ConfigureAwait(false);
        return profile;
    }

    public async Task UpdateAsync(CycleProfile profile, CancellationToken cancellationToken = default) {
        context.CycleProfiles.Update(profile);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task<CycleProfile?> GetByIdAsync(
        CycleProfileId id,
        UserId userId,
        bool includeDetails = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<CycleProfile> query = BuildQuery(includeDetails, asTracking)
            .Where(profile => profile.Id == id && profile.UserId == userId);

        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<CycleProfile?> GetCurrentAsync(
        UserId userId,
        bool includeDetails = false,
        CancellationToken cancellationToken = default) {
        IOrderedQueryable<CycleProfile> query = BuildQuery(includeDetails, asTracking: false)
            .Where(profile => profile.UserId == userId)
            .OrderByDescending(profile => profile.CreatedOnUtc);

        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<CycleProfileReadModel?> GetCurrentReadModelAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.CycleProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .OrderByDescending(profile => profile.CreatedOnUtc)
            .Select(profile => new CycleProfileReadModel(
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
                profile.BleedingEntries
                    .Select(entry => new BleedingEntryReadModel(
                        entry.Id.Value,
                        entry.CycleProfileId.Value,
                        entry.Date,
                        entry.Type,
                        entry.Flow,
                        entry.PainImpact,
                        entry.Notes))
                    .ToList(),
                profile.SymptomEntries
                    .Select(entry => new CycleSymptomEntryReadModel(
                        entry.Id.Value,
                        entry.CycleProfileId.Value,
                        entry.Date,
                        entry.Category,
                        entry.Intensity,
                        entry.Tags,
                        entry.Note))
                    .ToList(),
                profile.Factors
                    .Select(factor => new CycleFactorReadModel(
                        factor.Id.Value,
                        factor.CycleProfileId.Value,
                        factor.Type,
                        factor.StartDate,
                        factor.EndDate,
                        factor.Notes))
                    .ToList(),
                profile.FertilitySignals
                    .Select(signal => new FertilitySignalReadModel(
                        signal.Id.Value,
                        signal.CycleProfileId.Value,
                        signal.Date,
                        signal.BasalBodyTemperatureCelsius,
                        signal.OvulationTestResult,
                        signal.CervicalFluid,
                        signal.HadSex,
                        signal.Notes))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CycleProfile>> GetByUserAsync(
        UserId userId,
        bool includeDetails = false,
        CancellationToken cancellationToken = default) {
        IOrderedQueryable<CycleProfile> query = BuildQuery(includeDetails, asTracking: false)
            .Where(profile => profile.UserId == userId)
            .OrderByDescending(profile => profile.CreatedOnUtc);

        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private IQueryable<CycleProfile> BuildQuery(bool includeDetails, bool asTracking) {
        IQueryable<CycleProfile> query = asTracking
            ? context.CycleProfiles.AsQueryable()
            : context.CycleProfiles.AsNoTracking();

        if (includeDetails) {
            query = query
                .AsSplitQuery()
                .Include(profile => profile.Factors)
                .Include(profile => profile.BleedingEntries)
                .Include(profile => profile.SymptomEntries)
                .Include(profile => profile.FertilitySignals);
        }

        return query;
    }
}
