using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Domain.Enums;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public interface IDietologistInvitationReadRepository {
    Task<DietologistInvitation?> GetByIdAsync(
        DietologistInvitationId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<DietologistInvitation?> GetByClientAndStatusAsync(
        UserId clientUserId,
        DietologistInvitationStatus status,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<DietologistInvitation?> GetActiveByClientAsync(
        UserId clientUserId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<DietologistInvitation?> GetActiveByClientAndDietologistAsync(
        UserId clientUserId,
        UserId dietologistUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DietologistInvitation>> GetActiveByDietologistAsync(
        UserId dietologistUserId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveRelationshipAsync(
        UserId clientUserId,
        UserId dietologistUserId,
        CancellationToken cancellationToken = default);
}
