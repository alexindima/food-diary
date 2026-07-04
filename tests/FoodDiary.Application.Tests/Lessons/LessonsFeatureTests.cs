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
        var lesson = NutritionLesson.Create("Proteins", "Content", summary: null, "en",
            LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5);
        INutritionLessonRepository repo = CreateLessonRepository(lesson, hasProgress: false, out Func<bool> wasProgressAdded);
        var handler = new MarkLessonReadCommandHandler(repo, repo, new FixedDateTimeProvider());

        Result result = await handler.Handle(
            new MarkLessonReadCommand(Guid.NewGuid(), lesson.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(wasProgressAdded());
    }

    [Fact]
    public async Task MarkLessonRead_WhenAlreadyRead_ReturnsSuccessWithoutDuplicate() {
        var lesson = NutritionLesson.Create("Proteins", "Content", summary: null, "en",
            LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5);
        INutritionLessonRepository repo = CreateLessonRepository(lesson, hasProgress: true, out Func<bool> wasProgressAdded);
        var handler = new MarkLessonReadCommandHandler(repo, repo, new FixedDateTimeProvider());

        Result result = await handler.Handle(
            new MarkLessonReadCommand(Guid.NewGuid(), lesson.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.False(wasProgressAdded());
    }

    [Fact]
    public async Task MarkLessonRead_WhenLessonNotFound_ReturnsFailure() {
        INutritionLessonRepository repo = CreateLessonRepository(lesson: null, hasProgress: false);
        var handler = new MarkLessonReadCommandHandler(repo, repo, new FixedDateTimeProvider());

        Result result = await handler.Handle(
            new MarkLessonReadCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("NotFound", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MarkLessonRead_WithNullUserId_ReturnsFailure() {
        var handler = new MarkLessonReadCommandHandler(
            CreateLessonRepository(lesson: null, hasProgress: false),
            CreateLessonRepository(lesson: null, hasProgress: false),
            new FixedDateTimeProvider());

        Result result = await handler.Handle(
            new MarkLessonReadCommand(UserId: null, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetLessons_WithLocaleAndCategory_ReturnsSortedLessonsWithReadFlags() {
        var userId = UserId.New();
        NutritionLesson firstLesson = CreateLesson("Second", "ru", LessonCategory.Macronutrients, sortOrder: 2);
        NutritionLesson secondLesson = CreateLesson("First", "ru", LessonCategory.Macronutrients, sortOrder: 1);
        NutritionLesson otherCategoryLesson = CreateLesson("Hydration", "ru", LessonCategory.Hydration, sortOrder: 0);
        var progress = UserLessonProgress.Create(userId, secondLesson.Id, DateTime.UtcNow);
        INutritionLessonRepository repository = CreateLessonRepository(
            [firstLesson, secondLesson, otherCategoryLesson],
            [progress],
            out List<(string Locale, LessonCategory? Category)> localeRequests);
        var handler = new GetLessonsQueryHandler(repository);

        Result<IReadOnlyList<LessonSummaryModel>> result = await handler.Handle(
            new GetLessonsQuery(userId.Value, " RU ", "macronutrients"), CancellationToken.None);

        ResultAssert.Success(result);
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
        Assert.Equal(("ru", LessonCategory.Macronutrients), localeRequests.Single());
    }

    [Fact]
    public async Task GetLessons_WhenLocalizedLessonsAreMissing_FallsBackToEnglish() {
        var userId = UserId.New();
        NutritionLesson englishLesson = CreateLesson("English", "en", LessonCategory.NutritionBasics);
        INutritionLessonRepository repository = CreateLessonRepository(
            [englishLesson],
            [],
            out List<(string Locale, LessonCategory? Category)> localeRequests);
        var handler = new GetLessonsQueryHandler(repository);

        Result<IReadOnlyList<LessonSummaryModel>> result = await handler.Handle(
            new GetLessonsQuery(userId.Value, "fr", Category: null), CancellationToken.None);

        ResultAssert.Success(result);
        LessonSummaryModel lesson = Assert.Single(result.Value);
        Assert.Equal(englishLesson.Id.Value, lesson.Id);
        Assert.Equal([("fr", null), ("en", null)], localeRequests);
    }

    [Fact]
    public async Task GetLessons_WithUnknownCategory_IgnoresCategoryFilter() {
        INutritionLessonRepository repository = CreateLessonRepository(
            [],
            [],
            out List<(string Locale, LessonCategory? Category)> localeRequests);
        var handler = new GetLessonsQueryHandler(repository);

        Result<IReadOnlyList<LessonSummaryModel>> result = await handler.Handle(
            new GetLessonsQuery(Guid.NewGuid(), "en", "unknown"), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(result.Value);
        Assert.Equal(("en", null), localeRequests.Single());
    }

    [Fact]
    public async Task GetLessons_WithNullUserId_ReturnsFailure() {
        var handler = new GetLessonsQueryHandler(CreateLessonRepository([], []));

        Result<IReadOnlyList<LessonSummaryModel>> result = await handler.Handle(
            new GetLessonsQuery(UserId: null, "en", Category: null), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetLessonById_WhenLessonExists_ReturnsDetailWithReadState() {
        var userId = UserId.New();
        NutritionLesson lesson = CreateLesson("Protein basics", "en", LessonCategory.Macronutrients);
        var progress = UserLessonProgress.Create(userId, lesson.Id, DateTime.UtcNow);
        var handler = new GetLessonByIdQueryHandler(CreateLessonRepository([lesson], [progress]));

        Result<LessonDetailModel> result = await handler.Handle(
            new GetLessonByIdQuery(userId.Value, lesson.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(lesson.Id.Value, result.Value.Id);
        Assert.Equal("Protein basics", result.Value.Title);
        Assert.True(result.Value.IsRead);
    }

    [Fact]
    public async Task GetLessonById_WhenLessonIsMissing_ReturnsNotFound() {
        var handler = new GetLessonByIdQueryHandler(CreateLessonRepository([], []));

        Result<LessonDetailModel> result = await handler.Handle(
            new GetLessonByIdQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("NotFound", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetLessonById_WithNullUserId_ReturnsFailure() {
        var handler = new GetLessonByIdQueryHandler(CreateLessonRepository([], []));

        Result<LessonDetailModel> result = await handler.Handle(
            new GetLessonByIdQuery(UserId: null, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
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

    private static INutritionLessonRepository CreateLessonRepository(
        NutritionLesson? lesson,
        bool hasProgress) =>
        CreateLessonRepository(lesson, hasProgress, out _);

    private static INutritionLessonRepository CreateLessonRepository(
        NutritionLesson? lesson,
        bool hasProgress,
        out Func<bool> wasProgressAdded) =>
        CreateLessonRepository(lesson is null ? [] : [lesson], [], hasProgress, out wasProgressAdded, out _);

    private static INutritionLessonRepository CreateLessonRepository(
        IReadOnlyCollection<NutritionLesson> lessons,
        IReadOnlyCollection<UserLessonProgress> progress) =>
        CreateLessonRepository(lessons, progress, hasProgress: false, out _, out _);

    private static INutritionLessonRepository CreateLessonRepository(
        IReadOnlyCollection<NutritionLesson> lessons,
        IReadOnlyCollection<UserLessonProgress> progress,
        out List<(string Locale, LessonCategory? Category)> localeRequests) =>
        CreateLessonRepository(lessons, progress, hasProgress: false, out _, out localeRequests);

    private static INutritionLessonRepository CreateLessonRepository(
        IReadOnlyCollection<NutritionLesson> lessons,
        IReadOnlyCollection<UserLessonProgress> progress,
        bool hasProgress,
        out Func<bool> wasProgressAdded,
        out List<(string Locale, LessonCategory? Category)> localeRequests) {
        List<NutritionLesson> storedLessons = [.. lessons];
        List<UserLessonProgress> storedProgress = [.. progress];
        bool progressAdded = false;
        List<(string Locale, LessonCategory? Category)> capturedLocaleRequests = [];

        INutritionLessonRepository repository = Substitute.For<INutritionLessonRepository>();
        repository
            .GetByIdAsync(Arg.Any<NutritionLessonId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                NutritionLessonId id = call.Arg<NutritionLessonId>();
                return Task.FromResult(storedLessons.FirstOrDefault(lesson => lesson.Id == id));
            });
        repository
            .GetUserProgressForLessonAsync(
                Arg.Any<UserId>(),
                Arg.Any<NutritionLessonId>(),
                Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId userId = call.ArgAt<UserId>(0);
                NutritionLessonId lessonId = call.ArgAt<NutritionLessonId>(1);
                UserLessonProgress? foundProgress = hasProgress
                    ? UserLessonProgress.Create(userId, lessonId, DateTime.UtcNow)
                    : storedProgress.FirstOrDefault(progress => progress.UserId == userId && progress.LessonId == lessonId);

                return Task.FromResult(foundProgress);
            });
        repository
            .AddProgressAsync(Arg.Do<UserLessonProgress>(item => {
                progressAdded = true;
                storedProgress.Add(item);
            }), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<UserLessonProgress>()));
        repository
            .GetByLocaleAsync(Arg.Any<string>(), Arg.Any<LessonCategory?>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                string locale = call.ArgAt<string>(0);
                LessonCategory? category = call.ArgAt<LessonCategory?>(1);
                capturedLocaleRequests.Add((locale, category));
                IReadOnlyList<NutritionLesson> matchingLessons = storedLessons
                    .Where(lesson => string.Equals(lesson.Locale, locale, StringComparison.OrdinalIgnoreCase))
                    .Where(lesson => !category.HasValue || lesson.Category == category.Value)
                    .ToList();

                return Task.FromResult(matchingLessons);
            });
        repository
            .GetUserProgressAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId userId = call.Arg<UserId>();
                IReadOnlyList<UserLessonProgress> matchingProgress = storedProgress
                    .Where(item => item.UserId == userId)
                    .ToList();

                return Task.FromResult(matchingProgress);
            });

        wasProgressAdded = () => progressAdded;
        localeRequests = capturedLocaleRequests;
        return repository;
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(new(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc));
    }
}
