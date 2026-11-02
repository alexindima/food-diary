using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface IUserPrivacyStateReader {
    Task<UserPrivacyStateModel?> GetAsync(UserId userId, CancellationToken cancellationToken);
}
