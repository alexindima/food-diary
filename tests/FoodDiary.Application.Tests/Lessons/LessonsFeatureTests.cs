using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Application.Lessons.Commands.MarkLessonRead;
using FoodDiary.Application.Lessons.Mappings;
using FoodDiary.Application.Lessons.Models;
using FoodDiary.Application.Lessons.Queries.GetLessonById;
using FoodDiary.Application.Lessons.Queries.GetLessons;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Lessons;

[ExcludeFromCodeCoverage]
public class LessonsFeatureTests {
    [Fact]
    public async Task MarkLessonRead_WhenLessonExists_Succeeds() {
        var lesson = NutritionLesson.Create("Proteins", "Content", null, "en",
            LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5);
        var repo = new StubLessonRepository(lesson, hasProgress: false);
        var handler = new MarkLessonReadCommandHandler(repo, new FixedDateTimeProvider());

        Result result = await handler.Handle(
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

        Result result = await handler.Handle(
            new MarkLessonReadCommand(Guid.NewGuid(), lesson.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(repo.ProgressAdded);
    }

    [Fact]
    public async Task MarkLessonRead_WhenLessonNotFound_ReturnsFailure() {
        var repo = new StubLessonRepository(null, hasProgress: false);
        var handler = new MarkLessonReadCommandHandler(repo, new FixedDateTimeProvider());

        Result result = await handler.Handle(
            new MarkLessonReadCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MarkLessonRead_WithNullUserId_ReturnsFailure() {
        var handler = new MarkLessonReadCommandHandler(
            new StubLessonRepository(null, false), new FixedDateTimeProvider());

        Result result = await handler.Handle(
            new MarkLessonReadCommand(null, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetLessons_WithLocaleAndCategory_ReturnsSortedLessonsWithReadFlags() {
        var userId = UserId.New();
        NutritionLesson firstLesson = CreateLesson("Second", "ru", LessonCategory.Macronutrients, sortOrder: 2);
        NutritionLesson secondLesson = CreateLesson("First", "ru", LessonCategory.Macronutrients, sortOrder: 1);
        NutritionLesson otherCategoryLesson = CreateLesson("Hydration", "ru", LessonCategory.Hydration, sortOrder: 0);
        var progress = UserLessonProgress.Create(userId, secondLesson.Id, DateTime.UtcNow);
        var repository = new StubLessonRepository(
            [firstLesson, secondLesson, otherCategoryLesson],
            [progress]);
        var handler = new GetLessonsQueryHandler(repository);

        Result<IReadOnlyList<LessonSummaryModel>> result = await handler.Handle(
            new GetLessonsQuery(userId.Value, " RU ", "macronutrients"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Collection(
            result.Value,
            item => {
                Assert.Equal(secondLesson.Id.Value, item.Id);
                Assert.Equal("First", item.Title);
                Assert.True(item.IsRead);
            },
            item => {
                Assert.Equal(firstLesson.Id.Value, item.Id);
                Assert.Equal("Second", item.Title);
                Assert.False(item.IsRead);
            });
        Assert.Equal(("ru", LessonCategory.Macronutrients), repository.LocaleRequests.Single());
    }

    [Fact]
    public async Task GetLessons_WhenLocalizedLessonsAreMissing_FallsBackToEnglish() {
        var userId = UserId.New();
        NutritionLesson englishLesson = CreateLesson("English", "en", LessonCategory.NutritionBasics);
        var repository = new StubLessonRepository([englishLesson], []);
        var handler = new GetLessonsQueryHandler(repository);

        Result<IReadOnlyList<LessonSummaryModel>> result = await handler.Handle(
            new GetLessonsQuery(userId.Value, "fr", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        LessonSummaryModel lesson = Assert.Single(result.Value);
        Assert.Equal(englishLesson.Id.Value, lesson.Id);
        Assert.Equal([("fr", null), ("en", null)], repository.LocaleRequests);
    }

    [Fact]
    public async Task GetLessons_WithUnknownCategory_IgnoresCategoryFilter() {
        var repository = new StubLessonRepository([], []);
        var handler = new GetLessonsQueryHandler(repository);

        Result<IReadOnlyList<LessonSummaryModel>> result = await handler.Handle(
            new GetLessonsQuery(Guid.NewGuid(), "en", "unknown"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
        Assert.Equal(("en", null), repository.LocaleRequests.Single());
    }

    [Fact]
    public async Task GetLessons_WithNullUserId_ReturnsFailure() {
        var handler = new GetLessonsQueryHandler(new StubLessonRepository([], []));

        Result<IReadOnlyList<LessonSummaryModel>> result = await handler.Handle(
            new GetLessonsQuery(null, "en", null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetLessonById_WhenLessonExists_ReturnsDetailWithReadState() {
        var userId = UserId.New();
        NutritionLesson lesson = CreateLesson("Protein basics", "en", LessonCategory.Macronutrients);
        var progress = UserLessonProgress.Create(userId, lesson.Id, DateTime.UtcNow);
        var handler = new GetLessonByIdQueryHandler(new StubLessonRepository([lesson], [progress]));

        Result<LessonDetailModel> result = await handler.Handle(
            new GetLessonByIdQuery(userId.Value, lesson.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(lesson.Id.Value, result.Value.Id);
        Assert.Equal("Protein basics", result.Value.Title);
        Assert.True(result.Value.IsRead);
    }

    [Fact]
    public async Task GetLessonById_WhenLessonIsMissing_ReturnsNotFound() {
        var handler = new GetLessonByIdQueryHandler(new StubLessonRepository([], []));

        Result<LessonDetailModel> result = await handler.Handle(
            new GetLessonByIdQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetLessonById_WithNullUserId_ReturnsFailure() {
        var handler = new GetLessonByIdQueryHandler(new StubLessonRepository([], []));

        Result<LessonDetailModel> result = await handler.Handle(
            new GetLessonByIdQuery(null, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void LessonMappings_ToSummaryModel_MapsReadState() {
        NutritionLesson lesson = CreateLesson("Protein basics", "en", LessonCategory.Macronutrients);

        LessonSummaryModel model = lesson.ToSummaryModel(new HashSet<NutritionLessonId> { lesson.Id });

        Assert.Equal(lesson.Id.Value, model.Id);
        Assert.Equal("Protein basics", model.Title);
        Assert.Equal("Macronutrients", model.Category);
        Assert.Equal("Beginner", model.Difficulty);
        Assert.True(model.IsRead);
    }

    [Fact]
    public void LessonMappings_ToDetailModel_MapsContentAndReadState() {
        var lesson = NutritionLesson.Create(
            "Protein basics",
            "Detailed content",
            "Summary",
            "en",
            LessonCategory.Macronutrients,
            LessonDifficulty.Intermediate,
            7);

        LessonDetailModel model = lesson.ToDetailModel(isRead: false);

        Assert.Equal(lesson.Id.Value, model.Id);
        Assert.Equal("Detailed content", model.Content);
        Assert.Equal("Summary", model.Summary);
        Assert.Equal("Intermediate", model.Difficulty);
        Assert.False(model.IsRead);
    }

    private static NutritionLesson CreateLesson(
        string title,
        string locale,
        LessonCategory category,
        int sortOrder = 0) =>
        NutritionLesson.Create(title, "Content", $"{title} summary", locale, category, LessonDifficulty.Beginner, 5, sortOrder);

    [ExcludeFromCodeCoverage]
    private sealed class StubLessonRepository(
        IReadOnlyCollection<NutritionLesson> lessons,
        IReadOnlyCollection<UserLessonProgress> progress) : INutritionLessonRepository {
        private readonly List<NutritionLesson> _lessons = [.. lessons];
        private readonly List<UserLessonProgress> _progress = [.. progress];
        private readonly bool _hasProgress;

        public StubLessonRepository(NutritionLesson? lesson, bool hasProgress)
            : this(lesson is null ? [] : [lesson], []) {
            _hasProgress = hasProgress;
        }

        public bool ProgressAdded { get; private set; }
        public List<(string Locale, LessonCategory? Category)> LocaleRequests { get; } = [];

        public Task<NutritionLesson?> GetByIdAsync(NutritionLessonId id, CancellationToken ct = default) =>
            Task.FromResult(_lessons.FirstOrDefault(lesson => lesson.Id == id));

        public Task<UserLessonProgress?> GetUserProgressForLessonAsync(UserId userId, NutritionLessonId lessonId, CancellationToken ct = default) =>
            Task.FromResult(_hasProgress
                ? UserLessonProgress.Create(userId, lessonId, DateTime.UtcNow)
                : _progress.FirstOrDefault(progress => progress.UserId == userId && progress.LessonId == lessonId));

        public Task<UserLessonProgress> AddProgressAsync(UserLessonProgress progress, CancellationToken ct = default) {
            ProgressAdded = true;
            _progress.Add(progress);
            return Task.FromResult(progress);
        }

        public Task<IReadOnlyList<NutritionLesson>> GetByLocaleAsync(
            string locale,
            LessonCategory? category = null,
            CancellationToken ct = default) {
            LocaleRequests.Add((locale, category));
            var matchingLessons = _lessons
                .Where(lesson => string.Equals(lesson.Locale, locale, StringComparison.OrdinalIgnoreCase))
                .Where(lesson => !category.HasValue || lesson.Category == category.Value)
                .ToList();

            return Task.FromResult<IReadOnlyList<NutritionLesson>>(matchingLessons);
        }

        public Task<IReadOnlyList<UserLessonProgress>> GetUserProgressAsync(UserId userId, CancellationToken ct = default) {
            var matchingProgress = _progress
                .Where(item => item.UserId == userId)
                .ToList();

            return Task.FromResult<IReadOnlyList<UserLessonProgress>>(matchingProgress);
        }

        public Task<IReadOnlyList<NutritionLesson>> GetAllAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<NutritionLesson?> GetByIdTrackingAsync(NutritionLessonId id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(NutritionLesson lesson, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddRangeAsync(IReadOnlyCollection<NutritionLesson> lessons, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(NutritionLesson lesson, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(NutritionLesson lesson, CancellationToken ct = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(new(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc));
    }
}
