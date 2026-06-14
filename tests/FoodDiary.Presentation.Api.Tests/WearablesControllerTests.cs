using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Application.Wearables.Commands.ConnectWearable;
using FoodDiary.Application.Wearables.Commands.DisconnectWearable;
using FoodDiary.Application.Wearables.Commands.SyncWearableData;
using FoodDiary.Application.Wearables.Queries.GetWearableAuthUrl;
using FoodDiary.Application.Wearables.Queries.GetWearableConnections;
using FoodDiary.Application.Wearables.Queries.GetWearableDailySummary;
using FoodDiary.Presentation.Api.Features.Wearables;
using FoodDiary.Presentation.Api.Features.Wearables.Requests;
using FoodDiary.Presentation.Api.Features.Wearables.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class WearablesControllerTests {
    [Fact]
    public async Task GetConnections_SendsQueryAndReturnsConnections() {
        DateTime connectedAtUtc = DateTime.UtcNow.AddDays(-30);
        var model = new WearableConnectionModel("fitbit", "external-1", IsActive: true, LastSyncedAtUtc: null, connectedAtUtc);
        RecordingSender sender = new(Result.Success<IReadOnlyList<WearableConnectionModel>>([model]));
        WearablesController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetConnections(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        IReadOnlyList<WearableConnectionHttpResponse> response = Assert.IsAssignableFrom<IReadOnlyList<WearableConnectionHttpResponse>>(ok.Value);
        WearableConnectionHttpResponse item = Assert.Single(response);
        Assert.Equal("fitbit", item.Provider);
        GetWearableConnectionsQuery query = Assert.IsType<GetWearableConnectionsQuery>(sender.Request);
        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public async Task GetAuthUrl_SendsQueryAndReturnsAuthUrlResponse() {
        RecordingSender sender = new(Result.Success("https://wearable.example/oauth"));
        WearablesController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetAuthUrl(userId, "fitbit", "state-123");

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        WearableAuthUrlHttpResponse response = Assert.IsType<WearableAuthUrlHttpResponse>(ok.Value);
        Assert.Equal("https://wearable.example/oauth", response.AuthorizationUrl);
        GetWearableAuthUrlQuery query = Assert.IsType<GetWearableAuthUrlQuery>(sender.Request);
        Assert.Equal(userId, query.UserId);
        Assert.Equal("fitbit", query.Provider);
        Assert.Equal("state-123", query.State);
    }

    [Fact]
    public async Task Connect_SendsCommandAndReturnsConnection() {
        DateTime connectedAtUtc = DateTime.UtcNow;
        var model = new WearableConnectionModel("fitbit", "external-1", IsActive: true, LastSyncedAtUtc: null, connectedAtUtc);
        RecordingSender sender = new(Result.Success(model));
        WearablesController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var request = new ConnectWearableHttpRequest("auth-code", "state-123");

        IActionResult result = await controller.Connect(userId, "fitbit", request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        WearableConnectionHttpResponse response = Assert.IsType<WearableConnectionHttpResponse>(ok.Value);
        Assert.Equal("fitbit", response.Provider);
        ConnectWearableCommand command = Assert.IsType<ConnectWearableCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("fitbit", command.Provider);
        Assert.Equal("auth-code", command.Code);
    }

    [Fact]
    public async Task Disconnect_SendsCommandAndReturnsNoContent() {
        RecordingSender sender = new(Result.Success());
        WearablesController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.Disconnect(userId, "fitbit");

        Assert.IsType<NoContentResult>(result);
        DisconnectWearableCommand command = Assert.IsType<DisconnectWearableCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("fitbit", command.Provider);
    }

    [Fact]
    public async Task Sync_SendsCommandAndReturnsDailySummary() {
        DateTime date = new(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc);
        WearableDailySummaryModel model = CreateDailySummary(date);
        RecordingSender sender = new(Result.Success(model));
        WearablesController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.Sync(userId, "fitbit", date);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        WearableDailySummaryHttpResponse response = Assert.IsType<WearableDailySummaryHttpResponse>(ok.Value);
        Assert.Equal(date, response.Date);
        Assert.Equal(8500, response.Steps);
        SyncWearableDataCommand command = Assert.IsType<SyncWearableDataCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("fitbit", command.Provider);
        Assert.Equal(date, command.Date);
    }

    [Fact]
    public async Task GetDailySummary_SendsQueryAndReturnsDailySummary() {
        DateTime date = new(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc);
        WearableDailySummaryModel model = CreateDailySummary(date);
        RecordingSender sender = new(Result.Success(model));
        WearablesController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetDailySummary(userId, date);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        WearableDailySummaryHttpResponse response = Assert.IsType<WearableDailySummaryHttpResponse>(ok.Value);
        Assert.Equal(72, response.HeartRate);
        GetWearableDailySummaryQuery query = Assert.IsType<GetWearableDailySummaryQuery>(sender.Request);
        Assert.Equal(userId, query.UserId);
        Assert.Equal(date, query.Date);
    }

    private static WearablesController CreateController(RecordingSender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };

    private static WearableDailySummaryModel CreateDailySummary(DateTime date) =>
        new(date, Steps: 8500, HeartRate: 72, CaloriesBurned: 350, ActiveMinutes: 45, SleepMinutes: 420);
}
