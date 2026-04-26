using Microsoft.EntityFrameworkCore;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

public class CycleRepository(FoodDiaryDbContext context) : ICycleRepository {
    public async Task<Cycle> AddAsync(Cycle cycle, CancellationToken cancellationToken = default) {
        await context.Cycles.AddAsync(cycle, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return cycle;
    }

    public async Task UpdateAsync(Cycle cycle, CancellationToken cancellationToken = default) {
        context.Cycles.Update(cycle);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Cycle?> GetByIdAsync(
        CycleId id,
        UserId userId,
        bool includeDays = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        var query = BuildQuery(includeDays, asTracking)
            .Where(cycle => cycle.Id == id && cycle.UserId == userId);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Cycle?> GetLatestAsync(
        UserId userId,
        bool includeDays = false,
        CancellationToken cancellationToken = default) {
        var query = BuildQuery(includeDays, asTracking: false)
            .Where(cycle => cycle.UserId == userId)
            .OrderByDescending(cycle => cycle.StartDate)
            .ThenByDescending(cycle => cycle.CreatedOnUtc);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Cycle>> GetByUserAsync(
        UserId userId,
        bool includeDays = false,
        CancellationToken cancellationToken = default) {
        var query = BuildQuery(includeDays, asTracking: false)
            .Where(cycle => cycle.UserId == userId)
            .OrderByDescending(cycle => cycle.StartDate);

        return await query.ToListAsync(cancellationToken);
    }

    private IQueryable<Cycle> BuildQuery(bool includeDays, bool asTracking) {
        IQueryable<Cycle> query = asTracking
            ? context.Cycles.AsQueryable()
            : context.Cycles.AsNoTracking();

        if (includeDays) {
            query = query.Include(cycle => cycle.Days);
        }

        return query;
    }
}
