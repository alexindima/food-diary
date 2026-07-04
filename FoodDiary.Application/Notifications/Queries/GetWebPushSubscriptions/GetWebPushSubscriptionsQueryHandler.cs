using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Notifications.Mappings;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Queries.GetWebPushSubscriptions;

public sealed class GetWebPushSubscriptionsQueryHandler(
    IWebPushSubscriptionReadRepository webPushSubscriptionRepository,
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

        IReadOnlyList<WebPushSubscription> subscriptions = await webPushSubscriptionRepository.GetByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        DateTime utcNow = dateTimeProvider.GetUtcNow().UtcDateTime;
        WebPushSubscription[] activeSubscriptions = [.. subscriptions
            .Where(subscription => subscription.ExpirationTimeUtc > utcNow)];

        return Result.Success<IReadOnlyList<WebPushSubscriptionModel>>(
            activeSubscriptions.Select(subscription => subscription.ToModel()).ToList());
    }
}
