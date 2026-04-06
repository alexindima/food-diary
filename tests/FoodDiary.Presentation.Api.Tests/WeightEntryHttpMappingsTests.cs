using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Presentation.Api.Features.WeightEntries.Mappings;
using FoodDiary.Presentation.Api.Features.WeightEntries.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class WeightEntryHttpMappingsTests {
    [Fact]
    public void CreateRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);
        var request = new CreateWeightEntryHttpRequest(date, 75.5);

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(date, command.Date);
        Assert.Equal(75.5, command.Weight);
    }

    [Fact]
    public void UpdateRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);
        var request = new UpdateWeightEntryHttpRequest(date, 74.8);

        var command = request.ToCommand(userId, entryId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(entryId, command.WeightEntryId);
        Assert.Equal(date, command.Date);
        Assert.Equal(74.8, command.Weight);
    }

    [Fact]
    public void ToDeleteCommand_MapsUserIdAndEntryId() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        var command = entryId.ToDeleteCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(entryId, command.WeightEntryId);
    }

    [Fact]
    public void ToLatestQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        var query = userId.ToLatestQuery();

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void GetWeightEntriesHttpQuery_ToQuery_MapsAllFields() {
        var userId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;
        var httpQuery = new GetWeightEntriesHttpQuery(from, to, 10, "asc");

        var query = httpQuery.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(from, query.DateFrom);
        Assert.Equal(to, query.DateTo);
        Assert.Equal(10, query.Limit);
        Assert.False(query.Descending);
    }

    [Fact]
    public void GetWeightEntriesHttpQuery_ToQuery_DefaultSortIsDescending() {
        var userId = Guid.NewGuid();
        var httpQuery = new GetWeightEntriesHttpQuery();

        var query = httpQuery.ToQuery(userId);

        Assert.True(query.Descending);
    }

    [Fact]
    public void GetWeightSummariesHttpQuery_ToQuery_MapsAllFields() {
        var userId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;
        var httpQuery = new GetWeightSummariesHttpQuery(from, to, 7);

        var query = httpQuery.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(from, query.DateFrom);
        Assert.Equal(to, query.DateTo);
        Assert.Equal(7, query.QuantizationDays);
    }

    [Fact]
    public void WeightEntryModel_ToHttpResponse_MapsAllFields() {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var date = DateTime.UtcNow.Date;
        var model = new WeightEntryModel(id, userId, date, 75.5);

        var response = model.ToHttpResponse();

        Assert.Equal(id, response.Id);
        Assert.Equal(userId, response.UserId);
        Assert.Equal(date, response.Date);
        Assert.Equal(75.5, response.Weight);
    }

    [Fact]
    public void WeightEntrySummaryModel_ToHttpResponse_MapsAllFields() {
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        var model = new WeightEntrySummaryModel(from, to, 75.2);

        var response = model.ToHttpResponse();

        Assert.Equal(from, response.StartDate);
        Assert.Equal(to, response.EndDate);
        Assert.Equal(75.2, response.AverageWeight);
    }
}
