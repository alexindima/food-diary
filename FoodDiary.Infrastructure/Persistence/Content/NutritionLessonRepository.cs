using FoodDiary.Application.Lessons.Common;
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
        var query = context.Set<NutritionLesson>()
            .AsNoTracking()
            .Where(l => l.Locale == locale);

        if (category.HasValue) {
            query = query.Where(l => l.Category == category.Value);
        }

        return await query
            .OrderBy(l => l.Category)
            .ThenBy(l => l.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<NutritionLesson?> GetByIdAsync(
        NutritionLessonId id,
        CancellationToken cancellationToken = default) {
        return await context.Set<NutritionLesson>()
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<UserLessonProgress>> GetUserProgressAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.Set<UserLessonProgress>()
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserLessonProgress?> GetUserProgressForLessonAsync(
        UserId userId,
        NutritionLessonId lessonId,
        CancellationToken cancellationToken = default) {
        return await context.Set<UserLessonProgress>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId, cancellationToken);
    }

    public async Task<UserLessonProgress> AddProgressAsync(
        UserLessonProgress progress,
        CancellationToken cancellationToken = default) {
        context.Set<UserLessonProgress>().Add(progress);
        await context.SaveChangesAsync(cancellationToken);
        return progress;
    }
}
