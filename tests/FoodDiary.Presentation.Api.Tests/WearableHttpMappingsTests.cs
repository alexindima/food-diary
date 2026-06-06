using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Application.Wearables.Commands.ConnectWearable;
using FoodDiary.Application.Wearables.Commands.DisconnectWearable;
using FoodDiary.Application.Wearables.Commands.SyncWearableData;
using FoodDiary.Application.Wearables.Queries.GetWearableAuthUrl;
using FoodDiary.Application.Wearables.Queries.GetWearableConnections;
using FoodDiary.Application.Wearables.Queries.GetWearableDailySummary;
using FoodDiary.Presentation.Api.Features.Wearables.Mappings;
using FoodDiary.Presentation.Api.Features.Wearables.Requests;
using FoodDiary.Presentation.Api.Features.Wearables.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class WearableHttpMappingsTests {
    [Fact]
    public void ToQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        GetWearableConnectionsQuery query = WearableHttpMappings.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void ToAuthUrlQuery_MapsProviderAndState() {
        var userId = Guid.NewGuid();

        GetWearableAuthUrlQuery query = WearableHttpMappings.ToAuthUrlQuery(userId, "fitbit", "state123");

        Assert.Equal(userId, query.UserId);
        Assert.Equal("fitbit", query.Provider);
        Assert.Equal("state123", query.State);
    }

    [Fact]
    public void ToDailySummaryQuery_MapsUserIdAndDate() {
        var userId = Guid.NewGuid();
        var date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);

        GetWearableDailySummaryQuery query = WearableHttpMappings.ToDailySummaryQuery(userId, date);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(date, query.Date);
    }

    [Fact]
    public void ConnectWearableRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var request = new ConnectWearableHttpRequest("auth-code-123", "state-123");

        ConnectWearableCommand command = request.ToCommand(userId, "fitbit");

        Assert.Equal(userId, command.UserId);
        Assert.Equal("fitbit", command.Provider);
        Assert.Equal("auth-code-123", command.Code);
        Assert.Equal("state-123", command.State);
    }

    [Fact]
    public void ToDisconnectCommand_MapsUserIdAndProvider() {
        var userId = Guid.NewGuid();

        DisconnectWearableCommand command = WearableHttpMappings.ToDisconnectCommand(userId, "googlefit");

        Assert.Equal(userId, command.UserId);
        Assert.Equal("googlefit", command.Provider);
    }

    [Fact]
    public void ToSyncCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);

        SyncWearableDataCommand command = WearableHttpMappings.ToSyncCommand(userId, "fitbit", date);

        Assert.Equal(userId, command.UserId);
        Assert.Equal("fitbit", command.Provider);
        Assert.Equal(date, command.Date);
    }

    [Fact]
    public void WearableConnectionModel_ToHttpResponse_MapsAllFields() {
        DateTime syncedAt = DateTime.UtcNow;
        DateTime connectedAt = DateTime.UtcNow.AddDays(-30);
        var model = new WearableConnectionModel("fitbit", "ext-user-1", IsActive: true, syncedAt, connectedAt);

        WearableConnectionHttpResponse response = model.ToHttpResponse();

        Assert.Equal("fitbit", response.Provider);
        Assert.Equal("ext-user-1", response.ExternalUserId);
        Assert.True(response.IsActive);
        Assert.Equal(syncedAt, response.LastSyncedAtUtc);
        Assert.Equal(connectedAt, response.ConnectedAtUtc);
    }

    [Fact]
    public void WearableConnectionModelList_ToHttpResponse_MapsAllItems() {
        var models = new List<WearableConnectionModel> {
            new("fitbit", "ext-1", true, null, DateTime.UtcNow),
            new("googlefit", "ext-2", false, null, DateTime.UtcNow),
        };

        IReadOnlyList<WearableConnectionHttpResponse> responses = models.ToHttpResponse();

        Assert.Equal(2, responses.Count);
        Assert.Equal("fitbit", responses[0].Provider);
        Assert.Equal("googlefit", responses[1].Provider);
    }

    [Fact]
    public void WearableDailySummaryModel_ToHttpResponse_MapsAllFields() {
        var date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);
        var model = new WearableDailySummaryModel(date, 8500, 72, 350, 45, 420);

        WearableDailySummaryHttpResponse response = model.ToHttpResponse();

        Assert.Equal(date, response.Date);
        Assert.Equal(8500, response.Steps);
        Assert.Equal(72, response.HeartRate);
        Assert.Equal(350, response.CaloriesBurned);
        Assert.Equal(45, response.ActiveMinutes);
        Assert.Equal(420, response.SleepMinutes);
    }
}
