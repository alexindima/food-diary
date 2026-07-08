using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetMyDietologistRelationship;

public sealed class GetMyDietologistRelationshipQueryHandler(
    IDietologistInvitationReadService readService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetMyDietologistRelationshipQuery, Result<DietologistRelationshipModel?>> {
    public async Task<Result<DietologistRelationshipModel?>> Handle(
        GetMyDietologistRelationshipQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<DietologistRelationshipModel?>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        return await readService.GetMyRelationshipAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
