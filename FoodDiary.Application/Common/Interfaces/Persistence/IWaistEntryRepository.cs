using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Persistence;

public interface IWaistEntryRepository
{
    Task<WaistEntry> AddAsync(WaistEntry entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(WaistEntry entry, CancellationToken cancellationToken = default);

    Task DeleteAsync(WaistEntry entry, CancellationToken cancellationToken = default);

    Task<WaistEntry?> GetByIdAsync(
        WaistEntryId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<WaistEntry?> GetByDateAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WaistEntry>> GetEntriesAsync(
        UserId userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? limit,
        bool descending,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WaistEntry>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);
}
