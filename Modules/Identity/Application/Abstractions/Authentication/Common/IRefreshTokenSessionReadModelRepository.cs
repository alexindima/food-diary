using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Authentication.Common;

public interface IRefreshTokenSessionReadModelRepository {
    Task<IReadOnlyList<RefreshTokenSessionReadModel>> GetActiveReadModelsAsync(
        UserId userId, CancellationToken cancellationToken = default);
}
