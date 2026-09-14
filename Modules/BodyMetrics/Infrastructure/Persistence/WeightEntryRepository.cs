using Microsoft.EntityFrameworkCore;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.BodyMetrics.Infrastructure.Persistence;

public sealed class WeightEntryRepository(DbSet<WeightEntry> entries) : IWeightEntryReadModelRepository, IWeightEntryWriteRepository {
    public async Task<WeightEntry> AddAsync(WeightEntry entry, CancellationToken cancellationToken = default) {
        await entries.AddAsync(entry, cancellationToken).ConfigureAwait(false);
        return entry;
    }

    public Task UpdateAsync(WeightEntry entry, CancellationToken cancellationToken = default) {
        entries.Update(entry);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(WeightEntry entry, CancellationToken cancellationToken = default) {
        entries.Remove(entry);
        return Task.CompletedTask;
    }

    public async Task<WeightEntry?> GetByIdAsync(
        WeightEntryId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<WeightEntry> query = asTracking
            ? entries.AsQueryable()
            : entries.AsNoTracking();

        return await query.FirstOrDefaultAsync(
            entry => entry.Id == id && entry.UserId == userId,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<WeightEntry?> GetByDateAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default) {
        DateTime normalizedDate = date.Date;
        return await entries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                entry => entry.UserId == userId && entry.Date == normalizedDate,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WeightEntryReadModel>> GetEntryReadModelsAsync(
        UserId userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? limit,
        bool descending,
        CancellationToken cancellationToken = default) {
        IQueryable<WeightEntry> query = entries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId);

        if (dateFrom.HasValue) {
            DateTime from = dateFrom.Value.Date;
            query = query.Where(entry => entry.Date >= from);
        }

        if (dateTo.HasValue) {
            DateTime to = dateTo.Value.Date;
            query = query.Where(entry => entry.Date <= to);
        }

        query = descending
            ? query.OrderByDescending(entry => entry.Date).ThenByDescending(entry => entry.CreatedOnUtc)
            : query.OrderBy(entry => entry.Date).ThenBy(entry => entry.CreatedOnUtc);

        if (limit > 0) {
            query = query.Take(limit.Value);
        }

        return await query
            .Select(entry => new WeightEntryReadModel(entry.Id.Value, entry.UserId.Value, entry.Date, entry.WeightKg))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WeightEntryReadModel>> GetByPeriodReadModelsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        var from = DateTime.SpecifyKind(dateFrom, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(dateTo, DateTimeKind.Utc);

        return await entries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId && entry.Date >= from && entry.Date <= to)
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.CreatedOnUtc)
            .Select(entry => new WeightEntryReadModel(entry.Id.Value, entry.UserId.Value, entry.Date, entry.WeightKg))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
