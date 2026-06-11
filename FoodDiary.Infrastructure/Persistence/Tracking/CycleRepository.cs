using Microsoft.EntityFrameworkCore;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

public class CycleRepository(FoodDiaryDbContext context) : ICycleRepository {
    public async Task<CycleProfile> AddAsync(CycleProfile profile, CancellationToken cancellationToken = default) {
        await context.CycleProfiles.AddAsync(profile, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return profile;
    }

    public async Task UpdateAsync(CycleProfile profile, CancellationToken cancellationToken = default) {
        context.CycleProfiles.Update(profile);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
                .Include(profile => profile.Factors)
                .Include(profile => profile.BleedingEntries)
                .Include(profile => profile.SymptomEntries)
                .Include(profile => profile.FertilitySignals);
        }

        return query;
    }
}
