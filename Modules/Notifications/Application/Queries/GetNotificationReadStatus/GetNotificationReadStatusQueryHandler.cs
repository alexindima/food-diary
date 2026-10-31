using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Notifications.Application.Queries.GetNotificationReadStatus;

public sealed class GetNotificationReadStatusQueryHandler(
    INotificationReadStatusRepository repository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetNotificationReadStatusQuery, Result<IReadOnlyList<Guid>>> {
    public async Task<Result<IReadOnlyList<Guid>>> Handle(GetNotificationReadStatusQuery query, CancellationToken cancellationToken) {
        Result<UserId> userId = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId, currentUserAccessService, cancellationToken).ConfigureAwait(false);
        if (userId.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<Guid>>(userId);
        }

        NotificationId[] ids = [.. query.NotificationIds.Distinct().Select(id => new NotificationId(id))];
        IReadOnlyList<Guid> readIds = await repository.GetReadIdsAsync(userId.Value, ids, cancellationToken).ConfigureAwait(false);
        return Result.Success(readIds);
    }
}
