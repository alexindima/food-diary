using FoodDiary.Modules.Lessons.Domain.Contracts.Enums;
using FoodDiary.Testing;
using FoodDiary.Modules.Lessons.Application.Commands.CreateLesson;
using FoodDiary.Modules.Lessons.Application.Commands.DeleteLesson;
using FoodDiary.Modules.Lessons.Application.Commands.ImportLessons;
using FoodDiary.Modules.Lessons.Application.Commands.UpdateLesson;
using FoodDiary.Modules.Admin.Application.Commands.DeleteAdminLesson;
using FoodDiary.Modules.Admin.Application.Commands.UpdateAdminLesson;
using FoodDiary.Modules.Lessons.Application.Abstractions.Common;
using FoodDiary.Results;
using FoodDiary.Modules.Admin.Application.Models;

namespace FoodDiary.Modules.Admin.Application.Tests.Admin;

public partial class AdminFeatureTests {

    [Fact]
    public async Task UpdateAdminLessonHandler_WithEmptyLessonId_ReturnsValidationFailure() {
        var handler = new UpdateAdminLessonCommandHandler(
            RequestTestSender.Create(new CreateLessonCommandHandler(Substitute.For<INutritionLessonWriteRepository>()), new UpdateLessonCommandHandler(Substitute.For<INutritionLessonWriteRepository>()), new DeleteLessonCommandHandler(Substitute.For<INutritionLessonWriteRepository>()), new ImportLessonsCommandHandler(Substitute.For<INutritionLessonReadRepository>(), Substitute.For<INutritionLessonWriteRepository>())));

        Result<AdminLessonModel> result = await handler.Handle(
            new UpdateAdminLessonCommand(
                Guid.Empty,
                "Title",
                "Content",
                Summary: null,
                "en",
                LessonCategory.NutritionBasics.ToString(),
                LessonDifficulty.Beginner.ToString(),
                EstimatedReadMinutes: 5,
                SortOrder: 1),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Id", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAdminLessonHandler_WithEmptyLessonId_ReturnsValidationFailure() {
        var handler = new DeleteAdminLessonCommandHandler(
            RequestTestSender.Create(new CreateLessonCommandHandler(Substitute.For<INutritionLessonWriteRepository>()), new UpdateLessonCommandHandler(Substitute.For<INutritionLessonWriteRepository>()), new DeleteLessonCommandHandler(Substitute.For<INutritionLessonWriteRepository>()), new ImportLessonsCommandHandler(Substitute.For<INutritionLessonReadRepository>(), Substitute.For<INutritionLessonWriteRepository>())));

        Result result = await handler.Handle(new DeleteAdminLessonCommand(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Id", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

}
