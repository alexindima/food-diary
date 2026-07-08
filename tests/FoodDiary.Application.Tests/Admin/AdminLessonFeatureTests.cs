using FoodDiary.Application.Admin.Commands.CreateAdminLesson;
using FoodDiary.Application.Admin.Commands.DeleteAdminLesson;
using FoodDiary.Application.Admin.Commands.ImportAdminLessons;
using FoodDiary.Application.Admin.Commands.UpdateAdminLesson;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Queries.GetAdminLessons;
using FoodDiary.Application.Admin.Services;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Application.Abstractions.Lessons.Models;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.Admin.Models;
using FluentValidation.Results;

namespace FoodDiary.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public class AdminLessonFeatureTests {
    [Fact]
    public async Task CreateAdminLessonHandler_WithValidData_ReturnsSuccess() {
        var repo = new InMemoryLessonRepository();
        var handler = new CreateAdminLessonCommandHandler(repo);

        Result<AdminLessonModel> result = await handler.Handle(
            new CreateAdminLessonCommand(
                Title: "Basics of Nutrition",
                Content: "<p>Content here</p>",
                Summary: "A short summary",
                Locale: "ru",
                Category: "NutritionBasics",
                Difficulty: "Beginner",
                EstimatedReadMinutes: 5,
                SortOrder: 1),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("Basics of Nutrition", result.Value.Title);
        Assert.Equal("<p>Content here</p>", result.Value.Content);
        Assert.Equal("A short summary", result.Value.Summary);
        Assert.Equal("ru", result.Value.Locale);
        Assert.Equal("NutritionBasics", result.Value.Category);
        Assert.Equal("Beginner", result.Value.Difficulty);
        Assert.Equal(5, result.Value.EstimatedReadMinutes);
        Assert.Equal(1, result.Value.SortOrder);
        Assert.Single(repo.Lessons);
    }

    [Fact]
    public async Task CreateAdminLessonHandler_WithInvalidCategory_ReturnsFailure() {
        var repo = new InMemoryLessonRepository();
        var handler = new CreateAdminLessonCommandHandler(repo);

        Result<AdminLessonModel> result = await handler.Handle(
            new CreateAdminLessonCommand(
                Title: "Title",
                Content: "Content",
                Summary: null,
                Locale: "en",
                Category: "InvalidCategory",
                Difficulty: "Beginner",
                EstimatedReadMinutes: 5,
                SortOrder: 0),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("category", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAdminLessonHandler_WithInvalidDifficulty_ReturnsFailure() {
        var repo = new InMemoryLessonRepository();
        var handler = new CreateAdminLessonCommandHandler(repo);

        Result<AdminLessonModel> result = await handler.Handle(
            new CreateAdminLessonCommand(
                Title: "Title",
                Content: "Content",
                Summary: null,
                Locale: "en",
                Category: "NutritionBasics",
                Difficulty: "Expert",
                EstimatedReadMinutes: 5,
                SortOrder: 0),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("difficulty", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAdminLessonHandler_WithValidData_UpdatesLesson() {
        var lesson = NutritionLesson.Create("Old Title", "Old Content", summary: null, "en",
            LessonCategory.NutritionBasics, LessonDifficulty.Beginner, 3);
        var repo = new InMemoryLessonRepository(lesson);
        var handler = new UpdateAdminLessonCommandHandler(repo);

        Result<AdminLessonModel> result = await handler.Handle(
            new UpdateAdminLessonCommand(
                Id: lesson.Id.Value,
                Title: "New Title",
                Content: "<p>New HTML Content</p>",
                Summary: "Updated summary",
                Locale: "ru",
                Category: "Macronutrients",
                Difficulty: "Intermediate",
                EstimatedReadMinutes: 10,
                SortOrder: 5),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("New Title", result.Value.Title);
        Assert.Equal("<p>New HTML Content</p>", result.Value.Content);
        Assert.Equal("Updated summary", result.Value.Summary);
        Assert.Equal("ru", result.Value.Locale);
        Assert.Equal("Macronutrients", result.Value.Category);
        Assert.Equal("Intermediate", result.Value.Difficulty);
        Assert.Equal(10, result.Value.EstimatedReadMinutes);
        Assert.Equal(5, result.Value.SortOrder);
    }

    [Fact]
    public async Task UpdateAdminLessonHandler_WhenLessonNotFound_ReturnsFailure() {
        var repo = new InMemoryLessonRepository();
        var handler = new UpdateAdminLessonCommandHandler(repo);

        Result<AdminLessonModel> result = await handler.Handle(
            new UpdateAdminLessonCommand(
                Id: Guid.NewGuid(),
                Title: "Title",
                Content: "Content",
                Summary: null,
                Locale: "en",
                Category: "NutritionBasics",
                Difficulty: "Beginner",
                EstimatedReadMinutes: 5,
                SortOrder: 0),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Lesson.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAdminLessonHandler_WithInvalidCategory_ReturnsFailure() {
        var lesson = NutritionLesson.Create("Title", "Content", summary: null, "en",
            LessonCategory.NutritionBasics, LessonDifficulty.Beginner, 3);
        var repo = new InMemoryLessonRepository(lesson);
        var handler = new UpdateAdminLessonCommandHandler(repo);

        Result<AdminLessonModel> result = await handler.Handle(
            new UpdateAdminLessonCommand(
                Id: lesson.Id.Value,
                Title: "Title",
                Content: "Content",
                Summary: null,
                Locale: "en",
                Category: "BadCategory",
                Difficulty: "Beginner",
                EstimatedReadMinutes: 5,
                SortOrder: 0),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAdminLessonHandler_WithInvalidDifficulty_ReturnsFailure() {
        var lesson = NutritionLesson.Create("Title", "Content", summary: null, "en",
            LessonCategory.NutritionBasics, LessonDifficulty.Beginner, 3);
        var repo = new InMemoryLessonRepository(lesson);
        var handler = new UpdateAdminLessonCommandHandler(repo);

        Result<AdminLessonModel> result = await handler.Handle(
            new UpdateAdminLessonCommand(
                Id: lesson.Id.Value,
                Title: "Title",
                Content: "Content",
                Summary: null,
                Locale: "en",
                Category: "NutritionBasics",
                Difficulty: "Expert",
                EstimatedReadMinutes: 5,
                SortOrder: 0),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("difficulty", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAdminLessonHandler_WhenLessonExists_Succeeds() {
        var lesson = NutritionLesson.Create("Title", "Content", summary: null, "en",
            LessonCategory.NutritionBasics, LessonDifficulty.Beginner, 3);
        var repo = new InMemoryLessonRepository(lesson);
        var handler = new DeleteAdminLessonCommandHandler(repo);

        Result result = await handler.Handle(
            new DeleteAdminLessonCommand(lesson.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(repo.Lessons);
    }

    [Fact]
    public async Task DeleteAdminLessonHandler_WhenLessonNotFound_ReturnsFailure() {
        var repo = new InMemoryLessonRepository();
        var handler = new DeleteAdminLessonCommandHandler(repo);

        Result result = await handler.Handle(
            new DeleteAdminLessonCommand(Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Lesson.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetAdminLessonsHandler_ReturnsAllLessons() {
        var lesson1 = NutritionLesson.Create("Lesson 1", "Content 1", summary: null, "en",
            LessonCategory.NutritionBasics, LessonDifficulty.Beginner, 3);
        var lesson2 = NutritionLesson.Create("Lesson 2", "Content 2", "Summary", "ru",
            LessonCategory.Macronutrients, LessonDifficulty.Advanced, 10);
        var repo = new InMemoryLessonRepository(lesson1, lesson2);
        GetAdminLessonsQueryHandler handler = new(CreateAdminContentReadService(repo));

        Result<IReadOnlyList<AdminLessonModel>> result = await handler.Handle(new GetAdminLessonsQuery(), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task GetAdminLessonsHandler_WhenEmpty_ReturnsEmptyList() {
        var repo = new InMemoryLessonRepository();
        GetAdminLessonsQueryHandler handler = new(CreateAdminContentReadService(repo));

        Result<IReadOnlyList<AdminLessonModel>> result = await handler.Handle(new GetAdminLessonsQuery(), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task ImportAdminLessonsHandler_WithValidLessons_AddsAllAndReturnsModels() {
        var repo = new InMemoryLessonRepository();
        var handler = new ImportAdminLessonsCommandHandler(repo);

        Result<AdminLessonsImportModel> result = await handler.Handle(
            new ImportAdminLessonsCommand(
                Version: 1,
                Lessons: [
                    new ImportAdminLessonItem(" Intro ", " Content ", " Summary ", "EN", "NutritionBasics", "Beginner", 3, 2),
                    new ImportAdminLessonItem("Advanced", "Details", Summary: null, "ru", "Macronutrients", "Advanced", 8, 4),
                ]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.ImportedCount);
        Assert.Equal(2, repo.Lessons.Count);
        Assert.Equal("Intro", result.Value.Lessons[0].Title);
        Assert.Equal("en", result.Value.Lessons[0].Locale);
    }

    [Fact]
    public async Task ImportAdminLessonsHandler_WithInvalidCategory_ReturnsIndexedValidationFailure() {
        var handler = new ImportAdminLessonsCommandHandler(new InMemoryLessonRepository());

        Result<AdminLessonsImportModel> result = await handler.Handle(
            new ImportAdminLessonsCommand(
                Version: 1,
                Lessons: [
                    new ImportAdminLessonItem("Title", "Content", Summary: null, "en", "Unknown", "Beginner", 3, 0),
                ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("lessons[0].category", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportAdminLessonsHandler_WithInvalidDifficulty_ReturnsIndexedValidationFailure() {
        var handler = new ImportAdminLessonsCommandHandler(new InMemoryLessonRepository());

        Result<AdminLessonsImportModel> result = await handler.Handle(
            new ImportAdminLessonsCommand(
                Version: 1,
                Lessons: [
                    new ImportAdminLessonItem("Title", "Content", Summary: null, "en", "NutritionBasics", "Expert", 3, 0),
                ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("lessons[0].difficulty", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportAdminLessonsHandler_WithInvalidLessonState_ReturnsIndexedValidationFailure() {
        var handler = new ImportAdminLessonsCommandHandler(new InMemoryLessonRepository());

        Result<AdminLessonsImportModel> result = await handler.Handle(
            new ImportAdminLessonsCommand(
                Version: 1,
                Lessons: [
                    new ImportAdminLessonItem(" ", "Content", Summary: null, "en", "NutritionBasics", "Beginner", 3, 0),
                ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("lessons[0]", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateAdminLessonValidator_WithEmptyTitle_HasError() {
        var validator = new CreateAdminLessonCommandValidator();

        ValidationResult result = await validator.ValidateAsync(
            new CreateAdminLessonCommand("", "Content", Summary: null, "en", "NutritionBasics", "Beginner", 5, 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, "Title", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CreateAdminLessonValidator_WithEmptyContent_HasError() {
        var validator = new CreateAdminLessonCommandValidator();

        ValidationResult result = await validator.ValidateAsync(
            new CreateAdminLessonCommand("Title", "", Summary: null, "en", "NutritionBasics", "Beginner", 5, 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, "Content", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CreateAdminLessonValidator_WithZeroReadMinutes_HasError() {
        var validator = new CreateAdminLessonCommandValidator();

        ValidationResult result = await validator.ValidateAsync(
            new CreateAdminLessonCommand("Title", "Content", Summary: null, "en", "NutritionBasics", "Beginner", 0, 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, "EstimatedReadMinutes", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CreateAdminLessonValidator_WithValidData_Passes() {
        var validator = new CreateAdminLessonCommandValidator();

        ValidationResult result = await validator.ValidateAsync(
            new CreateAdminLessonCommand("Title", "Content", "Summary", "ru", "NutritionBasics", "Beginner", 5, 0));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UpdateAdminLessonValidator_WithEmptyId_HasError() {
        var validator = new UpdateAdminLessonCommandValidator();

        ValidationResult result = await validator.ValidateAsync(
            new UpdateAdminLessonCommand(Guid.Empty, "Title", "Content", Summary: null, "en", "NutritionBasics", "Beginner", 5, 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, "Id", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpdateAdminLessonValidator_WithValidData_Passes() {
        var validator = new UpdateAdminLessonCommandValidator();

        ValidationResult result = await validator.ValidateAsync(
            new UpdateAdminLessonCommand(Guid.NewGuid(), "Title", "Content", Summary: null, "en", "NutritionBasics", "Beginner", 5, 0));

        Assert.True(result.IsValid);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryLessonRepository : INutritionLessonRepository {
        private readonly List<NutritionLesson> _lessons = [];

        public IReadOnlyList<NutritionLesson> Lessons => _lessons;

        public InMemoryLessonRepository(params NutritionLesson[] lessons) {
            _lessons.AddRange(lessons);
        }

        public Task<IReadOnlyList<NutritionLesson>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NutritionLesson>>(_lessons);

        public Task<NutritionLesson?> GetByIdAsync(NutritionLessonId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_lessons.FirstOrDefault(l => l.Id == id));

        public Task<NutritionLesson?> GetByIdTrackingAsync(NutritionLessonId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_lessons.FirstOrDefault(l => l.Id == id));

        public Task AddAsync(NutritionLesson lesson, CancellationToken cancellationToken = default) {
            _lessons.Add(lesson);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IReadOnlyCollection<NutritionLesson> lessons, CancellationToken cancellationToken = default) {
            _lessons.AddRange(lessons);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(NutritionLesson lesson, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(NutritionLesson lesson, CancellationToken cancellationToken = default) {
            _lessons.Remove(lesson);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<LessonSummaryReadModel>> GetSummaryReadModelsByLocaleAsync(
            string locale,
            LessonCategory? category = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LessonSummaryReadModel>>([
                .. _lessons
                    .Where(lesson => string.Equals(lesson.Locale, locale, StringComparison.Ordinal)
                        && (!category.HasValue || lesson.Category == category.Value))
                    .Select(static lesson => new LessonSummaryReadModel(
                        lesson.Id.Value,
                        lesson.Title,
                        lesson.Summary,
                        lesson.Category.ToString(),
                        lesson.Difficulty.ToString(),
                        lesson.EstimatedReadMinutes,
                        lesson.SortOrder)),
            ]);

        public Task<IReadOnlyList<LessonAdminReadModel>> GetAdminReadModelsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LessonAdminReadModel>>([
                .. _lessons.Select(static lesson => new LessonAdminReadModel(
                    lesson.Id.Value,
                    lesson.Title,
                    lesson.Content,
                    lesson.Summary,
                    lesson.Locale,
                    lesson.Category.ToString(),
                    lesson.Difficulty.ToString(),
                    lesson.EstimatedReadMinutes,
                    lesson.SortOrder,
                    lesson.CreatedOnUtc,
                    lesson.ModifiedOnUtc)),
            ]);

        public Task<LessonDetailReadModel?> GetDetailReadModelByIdAsync(
            NutritionLessonId id,
            CancellationToken cancellationToken = default) {
            NutritionLesson? lesson = _lessons.FirstOrDefault(l => l.Id == id);
            return Task.FromResult(lesson is null
                ? null
                : new LessonDetailReadModel(
                    lesson.Id.Value,
                    lesson.Title,
                    lesson.Content,
                    lesson.Summary,
                    lesson.Category.ToString(),
                    lesson.Difficulty.ToString(),
                    lesson.EstimatedReadMinutes));
        }

        public Task<IReadOnlyList<NutritionLesson>> GetByLocaleAsync(string locale, LessonCategory? category = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> GetReadLessonIdsAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyList<UserLessonProgress>> GetUserProgressAsync(UserId userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> IsLessonReadAsync(UserId userId, NutritionLessonId lessonId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<UserLessonProgress?> GetUserProgressForLessonAsync(UserId userId, NutritionLessonId lessonId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UserLessonProgress> AddProgressAsync(UserLessonProgress progress, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private static IAdminContentReadService CreateAdminContentReadService(
        INutritionLessonReadModelRepository lessonRepository) =>
        new AdminContentReadService(
            lessonRepository,
            Substitute.For<IEmailTemplateReadModelRepository>(),
            Substitute.For<IAiPromptTemplateReadModelRepository>(),
            Substitute.For<IContentReportReadModelRepository>());
}
