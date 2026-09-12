using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserFastingReminderReadService {
    Task<IReadOnlyDictionary<UserId, UserFastingReminderModel>> GetReminderSettingsAsync(
        IReadOnlyCollection<UserId> userIds, CancellationToken cancellationToken = default);
}
