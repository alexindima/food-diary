using FoodDiary.Application.Wearables.Commands.ConnectWearable;
using FoodDiary.Application.Wearables.Commands.DisconnectWearable;
using FoodDiary.Application.Wearables.Commands.SyncWearableData;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Application.Wearables.Queries.GetWearableAuthUrl;
using FoodDiary.Application.Wearables.Queries.GetWearableConnections;
using FoodDiary.Application.Wearables.Queries.GetWearableDailySummary;
using FoodDiary.Presentation.Api.Features.Wearables.Requests;
using FoodDiary.Presentation.Api.Features.Wearables.Responses;

namespace FoodDiary.Presentation.Api.Features.Wearables.Mappings;

public static class WearableHttpMappings {
    public static GetWearableConnectionsQuery ToQuery(Guid userId) => new(userId);

    public static GetWearableAuthUrlQuery ToAuthUrlQuery(string provider, string state) => new(provider, state);

    public static GetWearableDailySummaryQuery ToDailySummaryQuery(Guid userId, DateTime date) => new(userId, date);

    public static ConnectWearableCommand ToCommand(
        this ConnectWearableHttpRequest request, Guid userId, string provider) =>
        new(userId, provider, request.Code);

    public static DisconnectWearableCommand ToDisconnectCommand(Guid userId, string provider) =>
        new(userId, provider);

    public static SyncWearableDataCommand ToSyncCommand(Guid userId, string provider, DateTime date) =>
        new(userId, provider, date);

    public static WearableConnectionHttpResponse ToHttpResponse(this WearableConnectionModel model) =>
        new(model.Provider, model.ExternalUserId, model.IsActive, model.LastSyncedAtUtc, model.ConnectedAtUtc);

    public static IReadOnlyList<WearableConnectionHttpResponse> ToHttpResponse(
        this IReadOnlyList<WearableConnectionModel> models) =>
        models.Select(m => m.ToHttpResponse()).ToList();

    public static WearableDailySummaryHttpResponse ToHttpResponse(this WearableDailySummaryModel model) =>
        new(model.Date, model.Steps, model.HeartRate, model.CaloriesBurned, model.ActiveMinutes, model.SleepMinutes);
}
