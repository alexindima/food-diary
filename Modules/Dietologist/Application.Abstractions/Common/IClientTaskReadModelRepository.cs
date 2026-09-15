using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public interface IClientTaskReadModelRepository {
    Task<IReadOnlyList<ClientTaskReadModel>> GetByClientAsync(
        UserId clientUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientTaskReadModel>> GetByDietologistAndClientAsync(
        UserId dietologistUserId,
        UserId clientUserId,
        CancellationToken cancellationToken = default);
}
