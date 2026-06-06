using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Notifications.Mappings;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Queries.GetWebPushSubscriptions;

public sealed class GetWebPushSubscriptionsQueryHandler(
    IWebPushSubscriptionRepository webPushSubscriptionRepository,
    IUserRepository userRepository,
    TimeProvider dateTimeProvider)
    : IQueryHandler<GetWebPushSubscriptionsQuery, Result<IReadOnlyList<WebPushSubscriptionModel>>> {
    public async Task<Result<IReadOnlyList<WebPushSubscriptionModel>>> Handle(
        GetWebPushSubscriptionsQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<WebPushSubscriptionModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<WebPushSubscriptionModel>>(accessError);
        }

        IReadOnlyList<WebPushSubscription> subscriptions = await webPushSubscriptionRepository.GetByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var expiredSubscriptions = subscriptions
            .Where(subscription => subscription.ExpirationTimeUtc <= dateTimeProvider.GetUtcNow().UtcDateTime)
            .ToList();

        if (expiredSubscriptions.Count > 0) {
            await webPushSubscriptionRepository.DeleteRangeAsync(expiredSubscriptions, cancellationToken).ConfigureAwait(false);
            subscriptions = subscriptions.Except(expiredSubscriptions).ToList();
        }

        return Result.Success<IReadOnlyList<WebPushSubscriptionModel>>(
            subscriptions.Select(subscription => subscription.ToModel()).ToList());
    }
}
