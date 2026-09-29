using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public interface IClientTaskReadModelRepository {
    Task<IReadOnlyList<ClientTaskReadModel>> GetByClientAsync(
        UserId clientUserId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    async Task<IReadOnlyList<ClientTaskReadModel>> GetByClientAsync(
        UserId clientUserId,
        int page,
        int limit,
        CancellationToken cancellationToken = default) =>
        (await GetByClientAsync(clientUserId, cancellationToken).ConfigureAwait(false)).Skip((page - 1) * limit).Take(limit).ToList();

    Task<IReadOnlyList<ClientTaskReadModel>> GetByDietologistAndClientAsync(
        UserId dietologistUserId,
        UserId clientUserId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    async Task<IReadOnlyList<ClientTaskReadModel>> GetByDietologistAndClientAsync(
        UserId dietologistUserId,
        UserId clientUserId,
        int page,
        int limit,
        CancellationToken cancellationToken = default) =>
        (await GetByDietologistAndClientAsync(dietologistUserId, clientUserId, cancellationToken).ConfigureAwait(false))
            .Skip((page - 1) * limit).Take(limit).ToList();
}
