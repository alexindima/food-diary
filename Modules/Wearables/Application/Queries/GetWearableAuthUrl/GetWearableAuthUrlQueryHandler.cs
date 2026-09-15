using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Wearables.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Wearables.Application.Common;
using FoodDiary.Modules.Wearables.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Wearables.Application.Queries.GetWearableAuthUrl;

public sealed class GetWearableAuthUrlQueryHandler(
    IEnumerable<IWearableClient> wearableClients,
    IWearableOAuthStateService stateService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetWearableAuthUrlQuery, Result<string>> {
    public async Task<Result<string>> Handle(
        GetWearableAuthUrlQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<string>(userIdResult);
        }

        Result<WearableProvider> providerResult = WearableProviderParser.Parse(query.Provider);
        if (providerResult.IsFailure) {
            return Result.Failure<string>(providerResult.Error);
        }

        WearableProvider provider = providerResult.Value;

        IWearableClient? client = wearableClients.FirstOrDefault(c => c.Provider == provider);
        if (client is null) {
            return Result.Failure<string>(WearableErrors.ProviderNotConfigured(query.Provider));
        }

        string state = stateService.CreateState(userIdResult.Value, provider, query.State);
        string url = client.GetAuthorizationUrl(state);
        return Result.Success(url);
    }
}
