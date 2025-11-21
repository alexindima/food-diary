using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Persistence;

public interface ICycleRepository
{
    Task<Cycle> AddAsync(Cycle cycle, CancellationToken cancellationToken = default);

    Task UpdateAsync(Cycle cycle, CancellationToken cancellationToken = default);

    Task<Cycle?> GetByIdAsync(
        CycleId id,
        UserId userId,
        bool includeDays = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<Cycle?> GetLatestAsync(
        UserId userId,
        bool includeDays = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Cycle>> GetByUserAsync(
        UserId userId,
        bool includeDays = false,
        CancellationToken cancellationToken = default);
}
