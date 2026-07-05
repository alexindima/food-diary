using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Abstractions.Users.Common;
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
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<WebPushSubscriptionModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<WebPushSubscriptionModel>>(accessError);
        }

        DateTime utcNow = dateTimeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyList<WebPushSubscriptionModel> activeSubscriptions = await webPushSubscriptionReadService
            .GetActiveSubscriptionsAsync(userId, utcNow, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(activeSubscriptions);
    }
}
