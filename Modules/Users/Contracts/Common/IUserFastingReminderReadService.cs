using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface IUserFastingReminderReadService {
    Task<IReadOnlyDictionary<UserId, UserFastingReminderModel>> GetReminderSettingsAsync(
        IReadOnlyCollection<UserId> userIds, CancellationToken cancellationToken = default);
}
