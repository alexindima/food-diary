using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Notifications.Application.Common;
using FoodDiary.Modules.Notifications.Application.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Queries.GetWebPushSubscriptions;

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
