using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public class HydrationEntryRepository : IHydrationEntryRepository
{
    private readonly FoodDiaryDbContext _context;

    public HydrationEntryRepository(FoodDiaryDbContext context)
    {
        _context = context;
    }

    public async Task<HydrationEntry> AddAsync(HydrationEntry entry, CancellationToken cancellationToken = default)
    {
        _context.HydrationEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task UpdateAsync(HydrationEntry entry, CancellationToken cancellationToken = default)
    {
        _context.HydrationEntries.Update(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(HydrationEntry entry, CancellationToken cancellationToken = default)
    {
        _context.HydrationEntries.Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<HydrationEntry?> GetByIdAsync(
        HydrationEntryId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.HydrationEntries.AsQueryable();
        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<HydrationEntry>> GetByDateAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken = default)
    {
        var dayStart = dateUtc.Date;
        var dayEnd = dayStart.AddDays(1);

        return await _context.HydrationEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Timestamp >= dayStart && x.Timestamp < dayEnd)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetDailyTotalAsync(UserId userId, DateTime dateUtc, CancellationToken cancellationToken = default)
    {
        var dayStart = dateUtc.Date;
        var dayEnd = dayStart.AddDays(1);

        return await _context.HydrationEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Timestamp >= dayStart && x.Timestamp < dayEnd)
            .SumAsync(x => x.AmountMl, cancellationToken);
    }
}
