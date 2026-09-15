using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetMyDietologistRelationship;

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
