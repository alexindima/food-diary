using FoodDiary.Results;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Common;

public interface IUserProfileReadService {
    Task<Result<UserModel>> GetUserAsync(UserId userId, CancellationToken cancellationToken);
    Task<Result<GoalsModel>> GetGoalsAsync(UserId userId, CancellationToken cancellationToken);
    Task<Result<UserDesiredWeightModel>> GetDesiredWeightAsync(UserId userId, CancellationToken cancellationToken);
    Task<Result<UserDesiredWaistModel>> GetDesiredWaistAsync(UserId userId, CancellationToken cancellationToken);
    Task<Result<NotificationPreferencesModel>> GetNotificationPreferencesAsync(UserId userId, CancellationToken cancellationToken);
}
