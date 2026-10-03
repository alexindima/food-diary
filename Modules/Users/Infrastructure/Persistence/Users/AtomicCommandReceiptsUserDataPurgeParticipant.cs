using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

internal sealed class AtomicCommandReceiptsUserDataPurgeParticipant(IAtomicCommandReceiptMaintenance maintenance) : IUserDataPurgeParticipant {
    public int Order => 140;
    public Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) =>
        maintenance.DeleteOwnerAsync(userId.Value, cancellationToken);
}
