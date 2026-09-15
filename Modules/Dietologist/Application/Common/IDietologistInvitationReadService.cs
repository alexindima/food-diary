using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Common;

public interface IDietologistInvitationReadService {
    Task<Result<IReadOnlyList<ClientSummaryModel>>> GetMyClientsAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<Result<DietologistRelationshipModel?>> GetMyRelationshipAsync(
        UserId userId,
        CancellationToken cancellationToken);
}
