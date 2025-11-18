using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Persistence;

public interface IWeightEntryRepository
{
    Task<WeightEntry> AddAsync(WeightEntry entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(WeightEntry entry, CancellationToken cancellationToken = default);

    Task DeleteAsync(WeightEntry entry, CancellationToken cancellationToken = default);

    Task<WeightEntry?> GetByIdAsync(
        WeightEntryId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<WeightEntry?> GetByDateAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WeightEntry>> GetEntriesAsync(
        UserId userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? limit,
        bool descending,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WeightEntry>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);
}
