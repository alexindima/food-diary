using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

public interface IRefreshTokenSessionReadModelRepository {
    Task<IReadOnlyList<RefreshTokenSessionReadModel>> GetActiveReadModelsAsync(
        UserId userId, CancellationToken cancellationToken = default);
}
