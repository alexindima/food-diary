using System.Reflection;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Application.Wearables.Wearables.Commands.ConnectWearable;
using FoodDiary.Application.Wearables.Wearables.Commands.DisconnectWearable;
using FoodDiary.Application.Wearables.Wearables.Commands.SyncWearableData;
using FoodDiary.Application.Wearables.Wearables.Queries.GetWearableAuthUrl;
using FoodDiary.Application.Wearables.Wearables.Queries.GetWearableConnections;
using FoodDiary.Application.Wearables.Wearables.Queries.GetWearableDailySummary;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Wearables;
using FoodDiary.Presentation.Api.Features.Wearables.Requests;
using FoodDiary.Presentation.Api.Features.Wearables.Responses;
using FoodDiary.Presentation.Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class WearablesControllerTests {
    private const string RequestId = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    private const string RequestHash = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";

    [Fact]
    public async Task GetConnections_SendsQueryAndReturnsConnections() {
        DateTime connectedAtUtc = DateTime.UtcNow.AddDays(-30);
        var model = new WearableConnectionModel("fitbit", "external-1", IsActive: true, LastSyncedAtUtc: null, connectedAtUtc);
        IRequest<Result<IReadOnlyList<WearableConnectionModel>>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success<IReadOnlyList<WearableConnectionModel>>([model]), request => sentRequest = request);
        WearablesController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetConnections(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        IReadOnlyList<WearableConnectionHttpResponse> response = Assert.IsAssignableFrom<IReadOnlyList<WearableConnectionHttpResponse>>(ok.Value);
        WearableConnectionHttpResponse item = Assert.Single(response);
        Assert.Equal("fitbit", item.Provider);
        GetWearableConnectionsQuery query = Assert.IsType<GetWearableConnectionsQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public async Task GetAuthUrl_SendsQueryAndReturnsAuthUrlResponse() {
        IRequest<Result<string>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success("https://wearable.example/oauth"), request => sentRequest = request);
        WearablesController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetAuthUrl(userId, "fitbit", "state-123");

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        WearableAuthUrlHttpResponse response = Assert.IsType<WearableAuthUrlHttpResponse>(ok.Value);
        Assert.Equal("https://wearable.example/oauth", response.AuthorizationUrl);
        GetWearableAuthUrlQuery query = Assert.IsType<GetWearableAuthUrlQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
        Assert.Equal("fitbit", query.Provider);
        Assert.Equal("state-123", query.State);
    }

    [Fact]
    public async Task Connect_SendsCommandAndReturnsConnection() {
        DateTime connectedAtUtc = DateTime.UtcNow;
        var model = new WearableConnectionModel("fitbit", "external-1", IsActive: true, LastSyncedAtUtc: null, connectedAtUtc);
        IRequest<Result<WearableConnectionModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        WearablesController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var request = new ConnectWearableHttpRequest("auth-code", "state-123");

        IActionResult result = await controller.Connect(userId, "fitbit", request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        WearableConnectionHttpResponse response = Assert.IsType<WearableConnectionHttpResponse>(ok.Value);
        Assert.Equal("fitbit", response.Provider);
        ConnectWearableCommand command = Assert.IsType<ConnectWearableCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("fitbit", command.Provider);
        Assert.Equal("auth-code", command.Code);
        Assert.Equal(RequestId, command.RequestId);
        Assert.Equal(RequestHash, command.RequestHash);
    }

    [Fact]
    public async Task Disconnect_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        WearablesController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.Disconnect(userId, "fitbit");

        Assert.IsType<NoContentResult>(result);
        DisconnectWearableCommand command = Assert.IsType<DisconnectWearableCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("fitbit", command.Provider);
    }

    [Fact]
    public async Task Sync_SendsCommandAndReturnsDailySummary() {
        DateTime date = new(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc);
        WearableDailySummaryModel model = CreateDailySummary(date);
        IRequest<Result<WearableDailySummaryModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        WearablesController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.Sync(userId, "fitbit", date);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        WearableDailySummaryHttpResponse response = Assert.IsType<WearableDailySummaryHttpResponse>(ok.Value);
        Assert.Equal(date, response.Date);
        Assert.Equal(8500, response.Steps);
        SyncWearableDataCommand command = Assert.IsType<SyncWearableDataCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("fitbit", command.Provider);
        Assert.Equal(date, command.Date);
    }

    [Fact]
    public async Task GetDailySummary_SendsQueryAndReturnsDailySummary() {
        DateTime date = new(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc);
        WearableDailySummaryModel model = CreateDailySummary(date);
        IRequest<Result<WearableDailySummaryModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        WearablesController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetDailySummary(userId, date);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        WearableDailySummaryHttpResponse response = Assert.IsType<WearableDailySummaryHttpResponse>(ok.Value);
        Assert.Equal(72, response.HeartRate);
        GetWearableDailySummaryQuery query = Assert.IsType<GetWearableDailySummaryQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
        Assert.Equal(date, query.Date);
    }

    private static WearablesController CreateController(ISender sender) {
        var httpContext = new DefaultHttpContext();
        MethodInfo setRequest = typeof(IdempotencyRequestContext).GetMethod(
            "SetRequest",
            BindingFlags.Static | BindingFlags.NonPublic) ??
            throw new InvalidOperationException("Idempotency request context setter was not found.");
        setRequest.Invoke(null, [httpContext, RequestId, RequestHash]);

        return new WearablesController(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = httpContext,
            },
        };
    }

    private static WearableDailySummaryModel CreateDailySummary(DateTime date) =>
        new(date, Steps: 8500, HeartRate: 72, CaloriesBurned: 350, ActiveMinutes: 45, SleepMinutes: 420);
}
