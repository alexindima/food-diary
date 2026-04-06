using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;
using FoodDiary.Presentation.Api.Features.WaistEntries.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class WaistEntryHttpMappingsTests {
    [Fact]
    public void CreateRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);
        var request = new CreateWaistEntryHttpRequest(date, 80.5);

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(date, command.Date);
        Assert.Equal(80.5, command.Circumference);
    }

    [Fact]
    public void UpdateRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);
        var request = new UpdateWaistEntryHttpRequest(date, 79.8);

        var command = request.ToCommand(userId, entryId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(entryId, command.WaistEntryId);
        Assert.Equal(date, command.Date);
        Assert.Equal(79.8, command.Circumference);
    }

    [Fact]
    public void ToDeleteCommand_MapsUserIdAndEntryId() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        var command = entryId.ToDeleteCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(entryId, command.WaistEntryId);
    }

    [Fact]
    public void ToLatestQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        var query = userId.ToLatestQuery();

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void GetWaistEntriesHttpQuery_ToQuery_MapsAllFields() {
        var userId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;
        var httpQuery = new GetWaistEntriesHttpQuery(from, to, 10, "asc");

        var query = httpQuery.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(from, query.DateFrom);
        Assert.Equal(to, query.DateTo);
        Assert.Equal(10, query.Limit);
        Assert.False(query.Descending);
    }

    [Fact]
    public void GetWaistEntriesHttpQuery_ToQuery_DefaultSortIsDescending() {
        var userId = Guid.NewGuid();
        var httpQuery = new GetWaistEntriesHttpQuery();

        var query = httpQuery.ToQuery(userId);

        Assert.True(query.Descending);
    }

    [Fact]
    public void GetWaistSummariesHttpQuery_ToQuery_MapsAllFields() {
        var userId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;
        var httpQuery = new GetWaistSummariesHttpQuery(from, to, 7);

        var query = httpQuery.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(from, query.DateFrom);
        Assert.Equal(to, query.DateTo);
        Assert.Equal(7, query.QuantizationDays);
    }

    [Fact]
    public void WaistEntryModel_ToHttpResponse_MapsAllFields() {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var date = DateTime.UtcNow.Date;
        var model = new WaistEntryModel(id, userId, date, 80.5);

        var response = model.ToHttpResponse();

        Assert.Equal(id, response.Id);
        Assert.Equal(userId, response.UserId);
        Assert.Equal(date, response.Date);
        Assert.Equal(80.5, response.Circumference);
    }

    [Fact]
    public void WaistEntrySummaryModel_ToHttpResponse_MapsAllFields() {
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        var model = new WaistEntrySummaryModel(from, to, 80.2);

        var response = model.ToHttpResponse();

        Assert.Equal(from, response.StartDate);
        Assert.Equal(to, response.EndDate);
        Assert.Equal(80.2, response.AverageCircumference);
    }
}
