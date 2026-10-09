using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

public interface IRefreshTokenSessionReadModelRepository {
    Task<IReadOnlyList<RefreshTokenSessionReadModel>> GetActiveReadModelsAsync(
        UserId userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefreshTokenSessionReadModel>> GetActivePageReadModelsAsync(
        UserId userId,
        int page,
        int limit,
        CancellationToken cancellationToken = default);

    Task<bool> IsActiveAsync(
        UserId userId,
        RefreshTokenSessionId sessionId,
        CancellationToken cancellationToken = default);
}
