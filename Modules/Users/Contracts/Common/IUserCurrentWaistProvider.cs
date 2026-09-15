using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface IUserCurrentWaistProvider {
    Task<double?> GetCurrentWaistAsync(UserId userId, CancellationToken cancellationToken = default);
}
