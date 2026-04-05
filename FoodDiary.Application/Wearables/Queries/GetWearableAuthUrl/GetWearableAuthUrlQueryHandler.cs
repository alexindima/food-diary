using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Wearables.Common;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Wearables.Queries.GetWearableAuthUrl;

public class GetWearableAuthUrlQueryHandler(IEnumerable<IWearableClient> wearableClients)
    : IQueryHandler<GetWearableAuthUrlQuery, Result<string>> {
    public Task<Result<string>> Handle(
        GetWearableAuthUrlQuery query,
        CancellationToken cancellationToken) {
        if (!Enum.TryParse<WearableProvider>(query.Provider, true, out var provider)) {
            return Task.FromResult(Result.Failure<string>(Errors.Wearable.InvalidProvider(query.Provider)));
        }

        var client = wearableClients.FirstOrDefault(c => c.Provider == provider);
        if (client is null) {
            return Task.FromResult(Result.Failure<string>(Errors.Wearable.ProviderNotConfigured(query.Provider)));
        }

        var url = client.GetAuthorizationUrl(query.State);
        return Task.FromResult(Result.Success(url));
    }
}
