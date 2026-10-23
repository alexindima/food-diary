using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Common;

/// <summary>Retains referenced catalog content through its owner before ordinary account purge.</summary>
public interface IUserContentRetentionParticipant {
    int Order { get; }
    /// <summary>Locks all source-owned content in the caller transaction before authoritative reference reads.</summary>
    Task LockOwnedContentAsync(UserId sourceUserId, CancellationToken cancellationToken);
    Task<bool> HasReferencedContentAsync(UserId sourceUserId, CancellationToken cancellationToken);
    Task RetainReferencedContentAsync(UserId sourceUserId, UserId targetUserId, CancellationToken cancellationToken);
}
