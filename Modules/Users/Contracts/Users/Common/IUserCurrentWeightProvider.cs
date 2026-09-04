using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserCurrentWeightProvider {
    Task<double?> GetCurrentWeightAsync(UserId userId, CancellationToken cancellationToken = default);
}
