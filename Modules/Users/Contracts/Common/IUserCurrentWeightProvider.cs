using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface IUserCurrentWeightProvider {
    Task<double?> GetCurrentWeightAsync(UserId userId, CancellationToken cancellationToken = default);
}
