using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Common;

/// <summary>Owner-side lifecycle operation within the coordinator's existing transaction. Never saves or commits.</summary>
public interface IUserDataPurgeParticipant {
    int Order { get; }
    Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken);
}
