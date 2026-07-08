using FoodDiary.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Queries.GetWebPushSubscriptions;

public sealed class GetWebPushSubscriptionsQueryHandler(
    IWebPushSubscriptionReadService webPushSubscriptionReadService,
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
        IReadOnlyList<WebPushSubscriptionModel> activeSubscriptions = await webPushSubscriptionReadService
            .GetActiveSubscriptionsAsync(userId, utcNow, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(activeSubscriptions);
    }
}
