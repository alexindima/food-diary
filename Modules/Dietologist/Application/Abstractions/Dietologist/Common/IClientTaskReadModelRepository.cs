using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public interface IClientTaskReadModelRepository {
    Task<IReadOnlyList<ClientTaskReadModel>> GetByClientAsync(
        UserId clientUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientTaskReadModel>> GetByDietologistAndClientAsync(
        UserId dietologistUserId,
        UserId clientUserId,
        CancellationToken cancellationToken = default);
}
