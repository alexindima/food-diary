using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserCurrentWaistProvider {
    Task<double?> GetCurrentWaistAsync(UserId userId, CancellationToken cancellationToken = default);
}
