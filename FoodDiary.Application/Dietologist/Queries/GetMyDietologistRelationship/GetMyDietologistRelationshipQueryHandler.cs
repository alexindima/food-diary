using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetMyDietologistRelationship;

public sealed class GetMyDietologistRelationshipQueryHandler(IDietologistInvitationReadService readService)
    : IQueryHandler<GetMyDietologistRelationshipQuery, Result<DietologistRelationshipModel?>> {
    public async Task<Result<DietologistRelationshipModel?>> Handle(
        GetMyDietologistRelationshipQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<DietologistRelationshipModel?>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        return await readService.GetMyRelationshipAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
