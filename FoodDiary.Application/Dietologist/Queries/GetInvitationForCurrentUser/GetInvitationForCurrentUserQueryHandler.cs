using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetInvitationForCurrentUser;

public sealed class GetInvitationForCurrentUserQueryHandler(
    IDietologistInvitationReadService readService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetInvitationForCurrentUserQuery, Result<DietologistInvitationForCurrentUserModel>> {
    public async Task<Result<DietologistInvitationForCurrentUserModel>> Handle(
        GetInvitationForCurrentUserQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<DietologistInvitationForCurrentUserModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        return await readService.GetForCurrentUserAsync(userId, query.InvitationId, cancellationToken).ConfigureAwait(false);
    }
}
