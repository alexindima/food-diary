using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public interface IClientTaskRepository {
    Task<ClientTask> AddAsync(ClientTask task, CancellationToken cancellationToken = default);

    Task<ClientTask?> GetByIdAsync(
        ClientTaskId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientTaskReadModel>> GetByClientAsync(
        UserId clientUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientTaskReadModel>> GetByDietologistAndClientAsync(
        UserId dietologistUserId,
        UserId clientUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientTask>> GetDueForReminderAsync(
        DateTime utcNow,
        DateTime dueBeforeUtc,
        int limit,
        CancellationToken cancellationToken = default);
}
