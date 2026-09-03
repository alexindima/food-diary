using FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;

namespace FoodDiary.Infrastructure.Persistence.Outbox;

/// <summary>Scoped persistence extension for the shared replay coordinator; implementations use its DbContext.</summary>
public interface IOutboxReplayStream {
    string Name { get; }
    int Order { get; }
    string? ReplayRejectionReason { get; }

    Task<IReadOnlyList<OutboxDeadLetterMessageModel>> ListAsync(int limit, CancellationToken cancellationToken = default);
    Task<OutboxReplayEntry?> FindAsync(Guid messageId, bool forUpdate, CancellationToken cancellationToken = default);
}
