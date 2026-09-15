using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Application.Abstractions.Models;
using FoodDiary.Modules.Notifications.Application.Mappings;
using FoodDiary.Modules.Notifications.Application.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Queries.GetWebPushSubscriptions;

public sealed class GetWebPushSubscriptionsQueryHandler(
    IWebPushSubscriptionReadModelRepository webPushSubscriptionRepository,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider dateTimeProvider)
    : IQueryHandler<GetWebPushSubscriptionsQuery, Result<IReadOnlyList<WebPushSubscriptionModel>>> {
    public async Task<Result<IReadOnlyList<WebPushSubscriptionModel>>> Handle(
        GetWebPushSubscriptionsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<WebPushSubscriptionModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        DateTime utcNow = dateTimeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyList<WebPushSubscriptionReadModel> subscriptions = await webPushSubscriptionRepository
            .GetByUserReadModelsAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<WebPushSubscriptionModel> activeSubscriptions = [.. subscriptions
            .Where(subscription => subscription.ExpirationTimeUtc > utcNow)
            .Select(subscription => subscription.ToModel())];

        return Result.Success(activeSubscriptions);
    }
}
