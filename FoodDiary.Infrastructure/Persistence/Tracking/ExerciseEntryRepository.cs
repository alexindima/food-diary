using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

internal sealed class ExerciseEntryRepository(FoodDiaryDbContext context) : IExerciseEntryRepository {
    public async Task<ExerciseEntry> AddAsync(ExerciseEntry entry, CancellationToken cancellationToken) {
        context.Set<ExerciseEntry>().Add(entry);
        await context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task UpdateAsync(ExerciseEntry entry, CancellationToken cancellationToken) {
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ExerciseEntry entry, CancellationToken cancellationToken) {
        context.Set<ExerciseEntry>().Remove(entry);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ExerciseEntry?> GetByIdAsync(
        ExerciseEntryId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        var query = asTracking
            ? context.Set<ExerciseEntry>().AsTracking()
            : context.Set<ExerciseEntry>().AsNoTracking();

        return await query.FirstOrDefaultAsync(
            e => e.Id == id && e.UserId == userId, cancellationToken);
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
            .ToListAsync(cancellationToken);
    }

    public async Task<double> GetTotalCaloriesBurnedAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default) {
        return await context.Set<ExerciseEntry>()
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.Date == date.Date)
            .SumAsync(e => e.CaloriesBurned, cancellationToken);
    }
}
