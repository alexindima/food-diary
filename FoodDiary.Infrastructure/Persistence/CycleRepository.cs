using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Infrastructure.Persistence;

public class CycleRepository : ICycleRepository
{
    private readonly FoodDiaryDbContext _context;

    public CycleRepository(FoodDiaryDbContext context) => _context = context;

    public async Task<Cycle> AddAsync(Cycle cycle, CancellationToken cancellationToken = default)
    {
        await _context.Cycles.AddAsync(cycle, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return cycle;
    }

    public async Task UpdateAsync(Cycle cycle, CancellationToken cancellationToken = default)
    {
        _context.Cycles.Update(cycle);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Cycle?> GetByIdAsync(
        CycleId id,
        UserId userId,
        bool includeDays = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(includeDays, asTracking)
            .Where(cycle => cycle.Id == id && cycle.UserId == userId);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Cycle?> GetLatestAsync(
        UserId userId,
        bool includeDays = false,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(includeDays, asTracking: false)
            .Where(cycle => cycle.UserId == userId)
            .OrderByDescending(cycle => cycle.StartDate)
            .ThenByDescending(cycle => cycle.CreatedOnUtc);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Cycle>> GetByUserAsync(
        UserId userId,
        bool includeDays = false,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(includeDays, asTracking: false)
            .Where(cycle => cycle.UserId == userId)
            .OrderByDescending(cycle => cycle.StartDate);

        return await query.ToListAsync(cancellationToken);
    }

    private IQueryable<Cycle> BuildQuery(bool includeDays, bool asTracking)
    {
        IQueryable<Cycle> query = asTracking
            ? _context.Cycles.AsQueryable()
            : _context.Cycles.AsNoTracking();

        if (includeDays)
        {
            query = query.Include(cycle => cycle.Days);
        }

        return query;
    }
}
