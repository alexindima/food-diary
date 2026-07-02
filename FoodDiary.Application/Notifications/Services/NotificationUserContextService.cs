using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Services;

public sealed class NotificationUserContextService(IUserRepository userRepository) : INotificationUserContextService {
    public async Task<Result<NotificationUserContext>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<NotificationUserContext>(accessError);
        }

        return Result.Success(new NotificationUserContext(user!.Id, user.HasPassword, user.Language));
    }
}
