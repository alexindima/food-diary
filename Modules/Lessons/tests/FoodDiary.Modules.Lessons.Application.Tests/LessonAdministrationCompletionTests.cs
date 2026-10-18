using FoodDiary.Modules.Lessons.Application.Abstractions.Common;
using FoodDiary.Modules.Lessons.Application.Commands.ImportLessons;
using FoodDiary.Modules.Lessons.Application.Commands.UpdateLesson;
using FoodDiary.Modules.Lessons.Contracts.Commands.ImportLessons;
using FoodDiary.Modules.Lessons.Contracts.Commands.UpdateLesson;
using FoodDiary.Modules.Lessons.Contracts.Common;
using FoodDiary.Modules.Lessons.Contracts.Models;
using FoodDiary.Modules.Lessons.Domain.Contracts.Enums;
using FoodDiary.Modules.Lessons.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Lessons.Domain.Entities.Content;
using FoodDiary.Results;

namespace FoodDiary.Modules.Lessons.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class LessonAdministrationCompletionTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateLesson_ReturnsRetainedCompletionCountAsync(bool isPublished) {
        var lesson = NutritionLesson.Create("Existing", "Content", summary: null, "en",
            LessonCategory.NutritionBasics, LessonDifficulty.Beginner, 5);
        INutritionLessonWriteRepository writer = Substitute.For<INutritionLessonWriteRepository>();
        INutritionLessonReadRepository reader = Substitute.For<INutritionLessonReadRepository>();
        using var cancellation = new CancellationTokenSource();
        writer.GetByIdTrackingAsync(lesson.Id, cancellation.Token).Returns(lesson);
        reader.GetCompletionCountsAsync(Arg.Any<IReadOnlyCollection<NutritionLessonId>>(), cancellation.Token)
            .Returns(Task.FromResult<IReadOnlyDictionary<NutritionLessonId, int>>(
                new Dictionary<NutritionLessonId, int> { [lesson.Id] = 2 }));
        var handler = new UpdateLessonCommandHandler(writer, reader);

        Result<LessonAdminReadModel> result = await handler.Handle(
            new UpdateLessonCommand(lesson.Id, "Edited", "Edited content", Summary: null, "en",
                LessonCategory.NutritionBasics, LessonDifficulty.Beginner, 5, 0, isPublished), cancellation.Token);

        ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal(2, result.Value.CompletedCount),
            () => Assert.Equal("Edited", result.Value.Title),
            () => Assert.Equal(isPublished, result.Value.IsPublished));
        await reader.Received(1).GetCompletionCountsAsync(
            Arg.Is<IReadOnlyCollection<NutritionLessonId>>(ids => ids.Count == 1 && ids.Contains(lesson.Id)), cancellation.Token);
    }

    [Fact]
    public async Task ImportLessons_ReusedAndNewSnapshotsRetainCountsWithoutDuplicatesAsync() {
        var lesson = NutritionLesson.Create("Existing", "Content", summary: null, "en",
            LessonCategory.NutritionBasics, LessonDifficulty.Beginner, 5);
        INutritionLessonWriteRepository writer = Substitute.For<INutritionLessonWriteRepository>();
        INutritionLessonReadRepository reader = Substitute.For<INutritionLessonReadRepository>();
        using var cancellation = new CancellationTokenSource();
        reader.GetAllAsync(cancellation.Token).Returns(Task.FromResult<IReadOnlyList<NutritionLesson>>([lesson]));
        reader.GetCompletionCountsAsync(Arg.Any<IReadOnlyCollection<NutritionLessonId>>(), cancellation.Token)
            .Returns(Task.FromResult<IReadOnlyDictionary<NutritionLessonId, int>>(
                new Dictionary<NutritionLessonId, int> { [lesson.Id] = 2 }));
        var existing = new LessonAdministrationItem("Existing", "Content", Summary: null, "en",
            LessonCategory.NutritionBasics, LessonDifficulty.Beginner, 5, 0);
        LessonAdministrationItem fresh = existing with { Title = "New" };
        var handler = new ImportLessonsCommandHandler(reader, writer);

        Result<IReadOnlyList<LessonAdminReadModel>> result = await handler.Handle(
            new ImportLessonsCommand([existing, fresh, existing]), cancellation.Token);

        ResultAssert.Success(result);
        Assert.Equal([2, 0, 2], result.Value.Select(item => item.CompletedCount));
        Assert.Equal(lesson.Id.Value, result.Value[0].Id);
        Assert.Equal(lesson.Id.Value, result.Value[2].Id);
        await writer.Received(1).AddRangeAsync(
            Arg.Is<IReadOnlyCollection<NutritionLesson>>(items => items.Count == 1), cancellation.Token);
        await reader.Received(1).GetCompletionCountsAsync(
            Arg.Is<IReadOnlyCollection<NutritionLessonId>>(ids => ids.Count == 2 && ids.Contains(lesson.Id)), cancellation.Token);
    }
}
