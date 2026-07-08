using FoodDiary.Results;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Common;

public interface IDietologistInvitationReadService {
    Task<Result<DietologistInvitationForCurrentUserModel>> GetForCurrentUserAsync(
        UserId userId,
        Guid invitationId,
        CancellationToken cancellationToken);

    Task<Result<InvitationModel>> GetByTokenAsync(
        UserId userId,
        Guid invitationId,
        CancellationToken cancellationToken);

    Task<Result<DietologistInfoModel?>> GetMyDietologistAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ClientSummaryModel>>> GetMyClientsAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<Result<DietologistRelationshipModel?>> GetMyRelationshipAsync(
        UserId userId,
        CancellationToken cancellationToken);
}
