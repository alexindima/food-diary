using FoodDiary.Application.Lessons.Commands.MarkLessonRead;
using FoodDiary.Application.Lessons.Models;
using FoodDiary.Application.Lessons.Queries.GetLessonById;
using FoodDiary.Application.Lessons.Queries.GetLessons;
using FoodDiary.Presentation.Api.Features.Lessons.Mappings;
using FoodDiary.Presentation.Api.Features.Lessons.Responses;
using FoodDiary.Presentation.Api.Features.Lessons.Requests;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class LessonHttpMappingsTests {
    [Fact]
    public void ToQuery_MapsAllFields() {
        var userId = Guid.NewGuid();

        GetLessonsQuery query = userId.ToQuery(new GetLessonsHttpQuery("ru", "nutrition", "Beginner", "protein", "shortest", 2, 20));

        Assert.Multiple(
            () => Assert.Equal(userId, query.UserId),
            () => Assert.Equal("ru", query.Locale),
            () => Assert.Equal("nutrition", query.Category),
            () => Assert.Equal("Beginner", query.Difficulty),
            () => Assert.Equal("protein", query.Search),
            () => Assert.Equal("shortest", query.Sort),
            () => Assert.Equal(2, query.Page),
            () => Assert.Equal(20, query.PageSize));
    }

    [Fact]
    public void ToGetByIdQuery_MapsUserIdAndLessonId() {
        var userId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();

        GetLessonByIdQuery query = userId.ToGetByIdQuery(lessonId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(lessonId, query.LessonId);
    }

    [Fact]
    public void ToMarkReadCommand_MapsUserIdAndLessonId() {
        var userId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();

        MarkLessonReadCommand command = userId.ToMarkReadCommand(lessonId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(lessonId, command.LessonId);
    }

    [Fact]
    public void LessonSummaryModelList_ToHttpResponse_MapsAllItems() {
        var models = new List<LessonSummaryModel> {
            new(Guid.NewGuid(), "Basics of Nutrition", "Learn the basics", "nutrition", "beginner", 5, false),
            new(Guid.NewGuid(), "Advanced Macros", null, "macros", "advanced", 10, true),
        };

        var model = new LessonPageModel(models, 1, 20, 2, 1, 31, 6, ["nutrition", "macros"]);
        LessonPageHttpResponse response = model.ToHttpResponse();
        IReadOnlyList<LessonSummaryHttpResponse> responses = response.Items;

        Assert.Multiple(
            () => Assert.Equal(2, responses.Count),
            () => Assert.Equal("Basics of Nutrition", responses[0].Title),
            () => Assert.False(responses[0].IsRead),
            () => Assert.True(responses[1].IsRead),
            () => Assert.Null(responses[1].Summary),
            () => Assert.Equal(31, response.TotalLessonCount),
            () => Assert.Equal(6, response.ReadLessonCount),
            () => Assert.Equal(["nutrition", "macros"], response.AvailableCategories));
    }

    [Fact]
    public void LessonDetailModel_ToHttpResponse_MapsAllFields() {
        var id = Guid.NewGuid();
        var model = new LessonDetailModel(id, "Title", "Full content", "Summary", "nutrition", "beginner", 5, IsRead: true);

        LessonDetailHttpResponse response = model.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Equal(id, response.Id),
            () => Assert.Equal("Title", response.Title),
            () => Assert.Equal("Full content", response.Content),
            () => Assert.Equal("Summary", response.Summary),
            () => Assert.True(response.IsRead));
    }
}
