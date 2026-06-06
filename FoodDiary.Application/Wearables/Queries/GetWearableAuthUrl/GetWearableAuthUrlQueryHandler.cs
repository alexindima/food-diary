using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Wearables.Queries.GetWearableAuthUrl;

public class GetWearableAuthUrlQueryHandler(
    IEnumerable<IWearableClient> wearableClients,
    IWearableOAuthStateService stateService)
    : IQueryHandler<GetWearableAuthUrlQuery, Result<string>> {
    public Task<Result<string>> Handle(
        GetWearableAuthUrlQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Task.FromResult(Result.Failure<string>(userIdResult.Error));
        }

        if (!Enum.TryParse<WearableProvider>(query.Provider, true, out WearableProvider provider)) {
            return Task.FromResult(Result.Failure<string>(Errors.Wearable.InvalidProvider(query.Provider)));
        }

        IWearableClient? client = wearableClients.FirstOrDefault(c => c.Provider == provider);
        if (client is null) {
            return Task.FromResult(Result.Failure<string>(Errors.Wearable.ProviderNotConfigured(query.Provider)));
        }

        string state = stateService.CreateState(userIdResult.Value, provider, query.State);
        string url = client.GetAuthorizationUrl(state);
        return Task.FromResult(Result.Success(url));
    }
}
