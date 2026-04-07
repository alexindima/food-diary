using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Lessons.Commands.MarkLessonRead;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Lessons;

public class LessonsFeatureTests {
    [Fact]
    public async Task MarkLessonRead_WhenLessonExists_Succeeds() {
        var lesson = NutritionLesson.Create("Proteins", "Content", null, "en",
            LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5);
        var repo = new StubLessonRepository(lesson, hasProgress: false);
        var handler = new MarkLessonReadCommandHandler(repo, new FixedDateTimeProvider());

        var result = await handler.Handle(
            new MarkLessonReadCommand(Guid.NewGuid(), lesson.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repo.ProgressAdded);
    }

    [Fact]
    public async Task MarkLessonRead_WhenAlreadyRead_ReturnsSuccessWithoutDuplicate() {
        var lesson = NutritionLesson.Create("Proteins", "Content", null, "en",
            LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5);
        var repo = new StubLessonRepository(lesson, hasProgress: true);
        var handler = new MarkLessonReadCommandHandler(repo, new FixedDateTimeProvider());

        var result = await handler.Handle(
            new MarkLessonReadCommand(Guid.NewGuid(), lesson.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(repo.ProgressAdded);
    }

    [Fact]
    public async Task MarkLessonRead_WhenLessonNotFound_ReturnsFailure() {
        var repo = new StubLessonRepository(null, hasProgress: false);
        var handler = new MarkLessonReadCommandHandler(repo, new FixedDateTimeProvider());

        var result = await handler.Handle(
            new MarkLessonReadCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task MarkLessonRead_WithNullUserId_ReturnsFailure() {
        var handler = new MarkLessonReadCommandHandler(
            new StubLessonRepository(null, false), new FixedDateTimeProvider());

        var result = await handler.Handle(
            new MarkLessonReadCommand(null, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private sealed class StubLessonRepository(NutritionLesson? lesson, bool hasProgress) : INutritionLessonRepository {
        public bool ProgressAdded { get; private set; }

        public Task<NutritionLesson?> GetByIdAsync(NutritionLessonId id, CancellationToken ct = default) =>
            Task.FromResult(lesson);

        public Task<UserLessonProgress?> GetUserProgressForLessonAsync(UserId userId, NutritionLessonId lessonId, CancellationToken ct = default) =>
            Task.FromResult(hasProgress
                ? UserLessonProgress.Create(userId, lessonId, DateTime.UtcNow)
                : null);

        public Task<UserLessonProgress> AddProgressAsync(UserLessonProgress progress, CancellationToken ct = default) {
            ProgressAdded = true;
            return Task.FromResult(progress);
        }

        public Task<IReadOnlyList<NutritionLesson>> GetByLocaleAsync(string locale, LessonCategory? category = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<NutritionLesson>> GetAllAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<NutritionLesson?> GetByIdTrackingAsync(NutritionLessonId id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<UserLessonProgress>> GetUserProgressAsync(UserId userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(NutritionLesson lesson, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(NutritionLesson lesson, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(NutritionLesson lesson, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider {
        public DateTime UtcNow => new(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);
    }
}
