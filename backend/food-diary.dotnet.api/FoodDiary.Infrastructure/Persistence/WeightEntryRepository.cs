using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Infrastructure.Persistence;

public class WeightEntryRepository : IWeightEntryRepository
{
    private readonly FoodDiaryDbContext _context;

    public WeightEntryRepository(FoodDiaryDbContext context) => _context = context;

    public async Task<WeightEntry> AddAsync(WeightEntry entry, CancellationToken cancellationToken = default)
    {
        await _context.WeightEntries.AddAsync(entry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task UpdateAsync(WeightEntry entry, CancellationToken cancellationToken = default)
    {
        _context.WeightEntries.Update(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(WeightEntry entry, CancellationToken cancellationToken = default)
    {
        _context.WeightEntries.Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<WeightEntry?> GetByIdAsync(
        WeightEntryId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = asTracking
            ? _context.WeightEntries.AsQueryable()
            : _context.WeightEntries.AsNoTracking();

        return await query.FirstOrDefaultAsync(
            entry => entry.Id == id && entry.UserId == userId,
            cancellationToken);
    }

    public async Task<WeightEntry?> GetByDateAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var normalizedDate = date.Date;
        return await _context.WeightEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                entry => entry.UserId == userId && entry.Date == normalizedDate,
                cancellationToken);
    }

    public async Task<IReadOnlyList<WeightEntry>> GetEntriesAsync(
        UserId userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? limit,
        bool descending,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WeightEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId);

        if (dateFrom.HasValue)
        {
            var from = dateFrom.Value.Date;
            query = query.Where(entry => entry.Date >= from);
        }

        if (dateTo.HasValue)
        {
            var to = dateTo.Value.Date;
            query = query.Where(entry => entry.Date <= to);
        }

        query = descending
            ? query.OrderByDescending(entry => entry.Date).ThenByDescending(entry => entry.CreatedOnUtc)
            : query.OrderBy(entry => entry.Date).ThenBy(entry => entry.CreatedOnUtc);

        if (limit.HasValue && limit.Value > 0)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
