using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Content;

internal sealed class NutritionLessonRepository(FoodDiaryDbContext context) : INutritionLessonRepository {
    public async Task<IReadOnlyList<NutritionLesson>> GetByLocaleAsync(
        string locale,
        LessonCategory? category = null,
        CancellationToken cancellationToken = default) {
        IQueryable<NutritionLesson> query = context.Set<NutritionLesson>()
            .AsNoTracking()
            .Where(l => l.Locale == locale);

        if (category.HasValue) {
            query = query.Where(l => l.Category == category.Value);
        }

        return await query
            .OrderBy(l => l.Category)
            .ThenBy(l => l.SortOrder)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<NutritionLesson>> GetAllAsync(
        CancellationToken cancellationToken = default) {
        return await context.Set<NutritionLesson>()
            .AsNoTracking()
            .OrderBy(l => l.Locale)
            .ThenBy(l => l.Category)
            .ThenBy(l => l.SortOrder)
            .ThenBy(l => l.CreatedOnUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<NutritionLesson?> GetByIdAsync(
        NutritionLessonId id,
        CancellationToken cancellationToken = default) {
        return await context.Set<NutritionLesson>()
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<NutritionLesson?> GetByIdTrackingAsync(
        NutritionLessonId id,
        CancellationToken cancellationToken = default) {
        return await context.Set<NutritionLesson>()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UserLessonProgress>> GetUserProgressAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.Set<UserLessonProgress>()
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserLessonProgress?> GetUserProgressForLessonAsync(
        UserId userId,
        NutritionLessonId lessonId,
        CancellationToken cancellationToken = default) {
        return await context.Set<UserLessonProgress>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserLessonProgress> AddProgressAsync(
        UserLessonProgress progress,
        CancellationToken cancellationToken = default) {
        await context.Set<UserLessonProgress>().AddAsync(progress, cancellationToken).ConfigureAwait(false);
        return progress;
    }

    public async Task AddAsync(
        NutritionLesson lesson,
        CancellationToken cancellationToken = default) {
        await context.Set<NutritionLesson>().AddAsync(lesson, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddRangeAsync(
        IReadOnlyCollection<NutritionLesson> lessons,
        CancellationToken cancellationToken = default) {
        await context.Set<NutritionLesson>().AddRangeAsync(lessons, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(
        NutritionLesson lesson,
        CancellationToken cancellationToken = default) {
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        NutritionLesson lesson,
        CancellationToken cancellationToken = default) {
        context.Set<NutritionLesson>().Remove(lesson);
        return Task.CompletedTask;
    }
}
