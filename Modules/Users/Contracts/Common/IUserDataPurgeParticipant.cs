using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Common;

/// <summary>Owner-side lifecycle operation within the coordinator's existing transaction. Never saves or commits.</summary>
public interface IUserDataPurgeParticipant {
    int Order { get; }
    Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken);
}
