using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

internal sealed class ExerciseEntryRepository(FoodDiaryDbContext context) : IExerciseEntryRepository {
    public Task<ExerciseEntry> AddAsync(ExerciseEntry entry, CancellationToken cancellationToken = default) {
        context.Set<ExerciseEntry>().Add(entry);
        return Task.FromResult(entry);
    }

    public Task UpdateAsync(ExerciseEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteAsync(ExerciseEntry entry, CancellationToken cancellationToken = default) {
        context.Set<ExerciseEntry>().Remove(entry);
        return Task.CompletedTask;
    }

    public async Task<ExerciseEntry?> GetByIdAsync(
        ExerciseEntryId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<ExerciseEntry> query = asTracking
            ? context.Set<ExerciseEntry>().AsTracking()
            : context.Set<ExerciseEntry>().AsNoTracking();

        return await query.FirstOrDefaultAsync(
            e => e.Id == id && e.UserId == userId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ExerciseEntry>> GetByDateRangeAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        return await context.Set<ExerciseEntry>()
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.Date >= dateFrom.Date && e.Date <= dateTo.Date)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedOnUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<double> GetTotalCaloriesBurnedAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default) {
        return await context.Set<ExerciseEntry>()
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.Date == date.Date)
            .SumAsync(e => e.CaloriesBurned, cancellationToken).ConfigureAwait(false);
    }
}
