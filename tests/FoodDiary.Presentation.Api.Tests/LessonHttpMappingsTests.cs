using FoodDiary.Application.Lessons.Models;
using FoodDiary.Presentation.Api.Features.Lessons.Mappings;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class LessonHttpMappingsTests {
    [Fact]
    public void ToQuery_MapsAllFields() {
        var userId = Guid.NewGuid();

        var query = userId.ToQuery("ru", "nutrition");

        Assert.Equal(userId, query.UserId);
        Assert.Equal("ru", query.Locale);
        Assert.Equal("nutrition", query.Category);
    }

    [Fact]
    public void ToGetByIdQuery_MapsUserIdAndLessonId() {
        var userId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();

        var query = userId.ToGetByIdQuery(lessonId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(lessonId, query.LessonId);
    }

    [Fact]
    public void ToMarkReadCommand_MapsUserIdAndLessonId() {
        var userId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();

        var command = userId.ToMarkReadCommand(lessonId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(lessonId, command.LessonId);
    }

    [Fact]
    public void LessonSummaryModelList_ToHttpResponse_MapsAllItems() {
        var models = new List<LessonSummaryModel> {
            new(Guid.NewGuid(), "Basics of Nutrition", "Learn the basics", "nutrition", "beginner", 5, false),
            new(Guid.NewGuid(), "Advanced Macros", null, "macros", "advanced", 10, true),
        };

        var responses = models.ToHttpResponse();

        Assert.Equal(2, responses.Count);
        Assert.Equal("Basics of Nutrition", responses[0].Title);
        Assert.False(responses[0].IsRead);
        Assert.True(responses[1].IsRead);
        Assert.Null(responses[1].Summary);
    }

    [Fact]
    public void LessonDetailModel_ToHttpResponse_MapsAllFields() {
        var id = Guid.NewGuid();
        var model = new LessonDetailModel(id, "Title", "Full content", "Summary", "nutrition", "beginner", 5, true);

        var response = model.ToHttpResponse();

        Assert.Equal(id, response.Id);
        Assert.Equal("Title", response.Title);
        Assert.Equal("Full content", response.Content);
        Assert.Equal("Summary", response.Summary);
        Assert.True(response.IsRead);
    }
}
