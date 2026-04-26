using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public interface IDietologistInvitationRepository {
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

    Task<DietologistInvitation> AddAsync(DietologistInvitation invitation, CancellationToken cancellationToken = default);
    Task UpdateAsync(DietologistInvitation invitation, CancellationToken cancellationToken = default);
}
