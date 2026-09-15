using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Domain.Entities;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public interface IClientTaskWriteRepository {
    Task<ClientTask> AddAsync(ClientTask task, CancellationToken cancellationToken = default);

    Task<ClientTask?> GetByIdAsync(
        ClientTaskId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientTask>> GetDueForReminderAsync(
        DateTime utcNow,
        DateTime dueBeforeUtc,
        int limit,
        CancellationToken cancellationToken = default);
}
