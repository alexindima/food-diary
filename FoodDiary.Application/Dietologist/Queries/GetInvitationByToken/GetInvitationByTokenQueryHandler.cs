using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetInvitationByToken;

public sealed class GetInvitationByTokenQueryHandler(
    IDietologistInvitationReadService readService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetInvitationByTokenQuery, Result<InvitationModel>> {
    public async Task<Result<InvitationModel>> Handle(GetInvitationByTokenQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<InvitationModel>(userIdResult);
        }

        return await readService.GetByTokenAsync(userIdResult.Value, query.InvitationId, cancellationToken).ConfigureAwait(false);
    }
}
