using FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;
using FoodDiary.Application.Abstractions.WaistEntries.Models;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Application.WaistEntries.Queries.GetLatestWaistEntry;
using FoodDiary.Application.WaistEntries.Queries.GetWaistEntries;
using FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;
using FoodDiary.Application.WaistEntries.Queries.GetWaistHistoryPageSummary;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;
using FoodDiary.Presentation.Api.Features.WaistEntries.Requests;
using FoodDiary.Presentation.Api.Features.WaistEntries.Responses;
using FoodDiary.Presentation.Api.Features.Users.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class WaistEntryHttpMappingsTests {
    [Fact]
    public void CreateRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);
        var request = new CreateWaistEntryHttpRequest(date, 80.5);

        CreateWaistEntryCommand command = request.ToCommand(userId);

        Assert.Multiple(
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal(date, command.Date),
            () => Assert.Equal(80.5, command.Circumference));
    }

    [Fact]
    public void UpdateRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);
        var request = new UpdateWaistEntryHttpRequest(date, 79.8);

        UpdateWaistEntryCommand command = request.ToCommand(userId, entryId);

        Assert.Multiple(
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal(entryId, command.WaistEntryId),
            () => Assert.Equal(date, command.Date),
            () => Assert.Equal(79.8, command.Circumference));
    }

    [Fact]
    public void ToDeleteCommand_MapsUserIdAndEntryId() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        DeleteWaistEntryCommand command = entryId.ToDeleteCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(entryId, command.WaistEntryId);
    }

    [Fact]
    public void ToLatestQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        GetLatestWaistEntryQuery query = userId.ToLatestQuery();

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void GetWaistEntriesHttpQuery_ToQuery_MapsAllFields() {
        var userId = Guid.NewGuid();
        DateTime from = DateTime.UtcNow.AddDays(-30);
        DateTime to = DateTime.UtcNow;
        var httpQuery = new GetWaistEntriesHttpQuery(from, to, 10, "asc");

        GetWaistEntriesQuery query = httpQuery.ToQuery(userId);

        Assert.Multiple(
            () => Assert.Equal(userId, query.UserId),
            () => Assert.Equal(from, query.DateFrom),
            () => Assert.Equal(to, query.DateTo),
            () => Assert.Equal(10, query.Limit),
            () => Assert.False(query.Descending));
    }

    [Fact]
    public void GetWaistEntriesHttpQuery_ToQuery_DefaultSortIsDescending() {
        var userId = Guid.NewGuid();
        var httpQuery = new GetWaistEntriesHttpQuery();

        GetWaistEntriesQuery query = httpQuery.ToQuery(userId);

        Assert.True(query.Descending);
    }

    [Fact]
    public void GetWaistSummariesHttpQuery_ToQuery_MapsAllFields() {
        var userId = Guid.NewGuid();
        DateTime from = DateTime.UtcNow.AddDays(-30);
        DateTime to = DateTime.UtcNow;
        var httpQuery = new GetWaistSummariesHttpQuery(from, to, 7);

        GetWaistSummariesQuery query = httpQuery.ToQuery(userId);

        Assert.Multiple(
            () => Assert.Equal(userId, query.UserId),
            () => Assert.Equal(from, query.DateFrom),
            () => Assert.Equal(to, query.DateTo),
            () => Assert.Equal(7, query.QuantizationDays));
    }

    [Fact]
    public void GetWaistHistoryPageSummaryHttpQuery_ToQuery_MapsAllFields() {
        var userId = Guid.NewGuid();
        DateTime from = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime to = from.AddDays(30);
        var httpQuery = new GetWaistHistoryPageSummaryHttpQuery(from, to, 5, 250);

        GetWaistHistoryPageSummaryQuery query = httpQuery.ToQuery(userId);

        Assert.Multiple(
            () => Assert.Equal(userId, query.UserId),
            () => Assert.Equal(from, query.DateFrom),
            () => Assert.Equal(to, query.DateTo),
            () => Assert.Equal(5, query.QuantizationDays),
            () => Assert.Equal(250, query.EntriesLimit));
    }

    [Fact]
    public void WaistEntryModel_ToHttpResponse_MapsAllFields() {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        DateTime date = DateTime.UtcNow.Date;
        var model = new WaistEntryModel(id, userId, date, 80.5);

        WaistEntryHttpResponse response = model.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Equal(id, response.Id),
            () => Assert.Equal(userId, response.UserId),
            () => Assert.Equal(date, response.Date),
            () => Assert.Equal(80.5, response.Circumference));
    }

    [Fact]
    public void WaistEntrySummaryModel_ToHttpResponse_MapsAllFields() {
        DateTime from = DateTime.UtcNow.AddDays(-7);
        DateTime to = DateTime.UtcNow;
        var model = new WaistEntrySummaryModel(from, to, 80.2);

        WaistEntrySummaryHttpResponse response = model.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Equal(from, response.StartDate),
            () => Assert.Equal(to, response.EndDate),
            () => Assert.Equal(80.2, response.AverageCircumference));
    }

    [Fact]
    public void WaistHistoryPageSummaryModel_ToHttpResponse_MapsNestedContract() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        DateTime date = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var model = new WaistHistoryPageSummaryModel(
            [new WaistEntryModel(entryId, userId, date, 80.5)],
            [new WaistEntrySummaryModel(date, date.AddDays(3), 80.2)],
            180,
            new UserDesiredWaistModel(75, 88.5, date),
            [new WaistGoalHistoryModel(goalId, 75, 88.5, 87, date, date.AddDays(10), "Cancelled")]);

        WaistHistoryPageSummaryHttpResponse response = model.ToHttpResponse();

        WaistEntryHttpResponse entry = Assert.Single(response.Entries);
        WaistEntrySummaryHttpResponse summary = Assert.Single(response.Summary);
        WaistGoalHistoryHttpResponse history = Assert.Single(response.GoalHistory);
        Assert.Multiple(
            () => Assert.Equal(entryId, entry.Id),
            () => Assert.Equal(80.2, summary.AverageCircumference),
            () => Assert.Equal(180, response.Height),
            () => Assert.Equal(75, response.Goal.DesiredWaist),
            () => Assert.Equal(goalId, history.Id),
            () => Assert.Equal("Cancelled", history.Status));
    }
}
