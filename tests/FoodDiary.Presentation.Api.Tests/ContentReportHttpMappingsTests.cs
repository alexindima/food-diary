using FoodDiary.Application.ContentReports.Models;
using FoodDiary.Presentation.Api.Features.ContentReports.Mappings;
using FoodDiary.Presentation.Api.Features.ContentReports.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class ContentReportHttpMappingsTests {
    [Fact]
    public void CreateContentReportRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var request = new CreateContentReportHttpRequest("Recipe", targetId, "Spam content");

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal("Recipe", command.TargetType);
        Assert.Equal(targetId, command.TargetId);
        Assert.Equal("Spam content", command.Reason);
    }

    [Fact]
    public void ContentReportModel_ToHttpResponse_MapsAllFields() {
        var id = Guid.NewGuid();
        var reporterId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var model = new ContentReportModel(
            id, reporterId, "Comment", targetId, "Offensive", "Pending", null, createdAt, null);

        var response = model.ToHttpResponse();

        Assert.Equal(id, response.Id);
        Assert.Equal(reporterId, response.ReporterId);
        Assert.Equal("Comment", response.TargetType);
        Assert.Equal(targetId, response.TargetId);
        Assert.Equal("Offensive", response.Reason);
        Assert.Equal("Pending", response.Status);
        Assert.Null(response.AdminNote);
        Assert.Equal(createdAt, response.CreatedAtUtc);
    }
}
