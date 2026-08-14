using FoodDiary.Application.Wearables.Wearables.Commands.ConnectWearable;
using FoodDiary.Application.Wearables.Wearables.Commands.DisconnectWearable;
using FoodDiary.Application.Wearables.Wearables.Commands.SyncWearableData;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Application.Wearables.Wearables.Queries.GetWearableAuthUrl;
using FoodDiary.Application.Wearables.Wearables.Queries.GetWearableConnections;
using FoodDiary.Application.Wearables.Wearables.Queries.GetWearableDailySummary;
using FoodDiary.Presentation.Api.Features.Wearables.Requests;
using FoodDiary.Presentation.Api.Features.Wearables.Responses;

namespace FoodDiary.Presentation.Api.Features.Wearables.Mappings;

public static class WearableHttpMappings {
    public static GetWearableConnectionsQuery ToQuery(Guid userId) => new(userId);

    public static GetWearableAuthUrlQuery ToAuthUrlQuery(Guid userId, string provider, string state) =>
        new(userId, provider, state);

    public static GetWearableDailySummaryQuery ToDailySummaryQuery(Guid userId, DateTime date) => new(userId, date);

    extension(ConnectWearableHttpRequest request) {
        public ConnectWearableCommand ToCommand(
        Guid userId, string provider) =>
                new(userId, provider, request.Code, request.State);
    }

    public static DisconnectWearableCommand ToDisconnectCommand(Guid userId, string provider) =>
        new(userId, provider);

    public static SyncWearableDataCommand ToSyncCommand(Guid userId, string provider, DateTime date) =>
        new(userId, provider, date);

    extension(WearableConnectionModel model) {
        public WearableConnectionHttpResponse ToHttpResponse() =>
                new(model.Provider, model.ExternalUserId, model.IsActive, model.LastSyncedAtUtc, model.ConnectedAtUtc);
    }

    extension(IReadOnlyList<WearableConnectionModel> models) {
        public IReadOnlyList<WearableConnectionHttpResponse> ToHttpResponse(
        ) =>
                models.Select(m => m.ToHttpResponse()).ToList();
    }

    extension(WearableDailySummaryModel model) {
        public WearableDailySummaryHttpResponse ToHttpResponse() =>
                new(model.Date, model.Steps, model.HeartRate, model.CaloriesBurned, model.ActiveMinutes, model.SleepMinutes);
    }
}
