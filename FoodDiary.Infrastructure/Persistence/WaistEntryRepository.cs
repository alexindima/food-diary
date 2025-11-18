using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public class WaistEntryRepository : IWaistEntryRepository
{
    private readonly FoodDiaryDbContext _context;

    public WaistEntryRepository(FoodDiaryDbContext context) => _context = context;

    public async Task<WaistEntry> AddAsync(WaistEntry entry, CancellationToken cancellationToken = default)
    {
        await _context.WaistEntries.AddAsync(entry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task UpdateAsync(WaistEntry entry, CancellationToken cancellationToken = default)
    {
        _context.WaistEntries.Update(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(WaistEntry entry, CancellationToken cancellationToken = default)
    {
        _context.WaistEntries.Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<WaistEntry?> GetByIdAsync(
        WaistEntryId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = asTracking
            ? _context.WaistEntries.AsQueryable()
            : _context.WaistEntries.AsNoTracking();

        return await query.FirstOrDefaultAsync(
            entry => entry.Id == id && entry.UserId == userId,
            cancellationToken);
    }

    public async Task<WaistEntry?> GetByDateAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var normalizedDate = date.Date;
        return await _context.WaistEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                entry => entry.UserId == userId && entry.Date == normalizedDate,
                cancellationToken);
    }

    public async Task<IReadOnlyList<WaistEntry>> GetEntriesAsync(
        UserId userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? limit,
        bool descending,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WaistEntries
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

    public async Task<IReadOnlyList<WaistEntry>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default)
    {
        var from = DateTime.SpecifyKind(dateFrom, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(dateTo, DateTimeKind.Utc);

        return await _context.WaistEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId && entry.Date >= from && entry.Date <= to)
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.CreatedOnUtc)
            .ToListAsync(cancellationToken);
    }
}
