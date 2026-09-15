using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Domain.Enums;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public interface IDietologistInvitationReadModelRepository {
    Task<DietologistInvitationReadModel?> GetByIdReadModelAsync(
        DietologistInvitationId id,
        CancellationToken cancellationToken = default);

    Task<DietologistInvitationReadModel?> GetByClientAndStatusReadModelAsync(
        UserId clientUserId,
        DietologistInvitationStatus status,
        CancellationToken cancellationToken = default);

    Task<DietologistInvitationReadModel?> GetActiveByClientReadModelAsync(
        UserId clientUserId,
        CancellationToken cancellationToken = default);

    Task<DietologistInvitationReadModel?> GetActiveByClientAndDietologistReadModelAsync(
        UserId clientUserId,
        UserId dietologistUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DietologistInvitationReadModel>> GetActiveByDietologistReadModelsAsync(
        UserId dietologistUserId,
        CancellationToken cancellationToken = default);
}
