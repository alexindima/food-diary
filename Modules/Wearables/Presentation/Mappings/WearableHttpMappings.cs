using FoodDiary.Modules.Wearables.Application.Commands.ConnectWearable;
using FoodDiary.Modules.Wearables.Application.Commands.DisconnectWearable;
using FoodDiary.Modules.Wearables.Application.Commands.SyncWearableData;
using FoodDiary.Modules.Wearables.Application.Abstractions.Models;
using FoodDiary.Modules.Wearables.Application.Queries.GetWearableAuthUrl;
using FoodDiary.Modules.Wearables.Application.Queries.GetWearableConnections;
using FoodDiary.Modules.Wearables.Application.Queries.GetWearableDailySummary;
using FoodDiary.Modules.Wearables.Presentation.Requests;
using FoodDiary.Modules.Wearables.Presentation.Responses;

namespace FoodDiary.Modules.Wearables.Presentation.Mappings;

public static class WearableHttpMappings {
    public static GetWearableConnectionsQuery ToQuery(Guid userId) => new(userId);

    public static GetWearableAuthUrlQuery ToAuthUrlQuery(Guid userId, string provider, string state) =>
        new(userId, provider, state);

    public static GetWearableDailySummaryQuery ToDailySummaryQuery(Guid userId, DateTime date) => new(userId, date);

    extension(ConnectWearableHttpRequest request) {
        public ConnectWearableCommand ToCommand(
        Guid userId, string provider, string requestId, string requestHash) =>
                new(userId, provider, request.Code, request.State, requestId, requestHash);
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
