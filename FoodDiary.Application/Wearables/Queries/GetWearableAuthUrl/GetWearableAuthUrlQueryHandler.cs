using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Wearables.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Wearables.Queries.GetWearableAuthUrl;

public sealed class GetWearableAuthUrlQueryHandler(
    IEnumerable<IWearableClient> wearableClients,
    IWearableOAuthStateService stateService)
    : IQueryHandler<GetWearableAuthUrlQuery, Result<string>> {
    public Task<Result<string>> Handle(
        GetWearableAuthUrlQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Task.FromResult(UserIdParser.ToFailure<string>(userIdResult));
        }

        Result<WearableProvider> providerResult = WearableProviderParser.Parse(query.Provider);
        if (providerResult.IsFailure) {
            return Task.FromResult(Result.Failure<string>(providerResult.Error));
        }

        WearableProvider provider = providerResult.Value;

        IWearableClient? client = wearableClients.FirstOrDefault(c => c.Provider == provider);
        if (client is null) {
            return Task.FromResult(Result.Failure<string>(Errors.Wearable.ProviderNotConfigured(query.Provider)));
        }

        string state = stateService.CreateState(userIdResult.Value, provider, query.State);
        string url = client.GetAuthorizationUrl(state);
        return Task.FromResult(Result.Success(url));
    }
}
