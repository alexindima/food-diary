using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Wearables.Queries.GetWearableAuthUrl;

public class GetWearableAuthUrlQueryHandler(
    IEnumerable<IWearableClient> wearableClients,
    IWearableOAuthStateService stateService)
    : IQueryHandler<GetWearableAuthUrlQuery, Result<string>> {
    public Task<Result<string>> Handle(
        GetWearableAuthUrlQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Task.FromResult(Result.Failure<string>(userIdResult.Error));
        }

        if (!Enum.TryParse<WearableProvider>(query.Provider, true, out var provider)) {
            return Task.FromResult(Result.Failure<string>(Errors.Wearable.InvalidProvider(query.Provider)));
        }

        var client = wearableClients.FirstOrDefault(c => c.Provider == provider);
        if (client is null) {
            return Task.FromResult(Result.Failure<string>(Errors.Wearable.ProviderNotConfigured(query.Provider)));
        }

        var state = stateService.CreateState(userIdResult.Value, provider, query.State);
        var url = client.GetAuthorizationUrl(state);
        return Task.FromResult(Result.Success(url));
    }
}
