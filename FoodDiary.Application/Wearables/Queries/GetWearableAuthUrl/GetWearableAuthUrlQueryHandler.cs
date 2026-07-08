using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Wearables.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Wearables.Queries.GetWearableAuthUrl;

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
            return Result.Failure<string>(Errors.Wearable.ProviderNotConfigured(query.Provider));
        }

        string state = stateService.CreateState(userIdResult.Value, provider, query.State);
        string url = client.GetAuthorizationUrl(state);
        return Result.Success(url);
    }
}
