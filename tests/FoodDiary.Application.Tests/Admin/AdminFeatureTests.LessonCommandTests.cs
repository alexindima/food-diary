using FoodDiary.Application.Admin.Commands.DeleteAdminLesson;
using FoodDiary.Application.Admin.Commands.UpdateAdminLesson;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Results;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Tests.Admin;

public partial class AdminFeatureTests {

    [Fact]
    public async Task UpdateAdminLessonHandler_WithEmptyLessonId_ReturnsValidationFailure() {
        var handler = new UpdateAdminLessonCommandHandler(Substitute.For<INutritionLessonWriteRepository>());

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
        var handler = new DeleteAdminLessonCommandHandler(Substitute.For<INutritionLessonWriteRepository>());

        Result result = await handler.Handle(new DeleteAdminLessonCommand(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Id", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

}
