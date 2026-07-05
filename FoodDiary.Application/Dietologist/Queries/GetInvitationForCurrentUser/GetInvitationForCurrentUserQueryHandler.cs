using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetInvitationForCurrentUser;

public sealed class GetInvitationForCurrentUserQueryHandler(IDietologistInvitationReadService readService)
    : IQueryHandler<GetInvitationForCurrentUserQuery, Result<DietologistInvitationForCurrentUserModel>> {
    public async Task<Result<DietologistInvitationForCurrentUserModel>> Handle(
        GetInvitationForCurrentUserQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        return await readService.GetForCurrentUserAsync(userId, query.InvitationId, cancellationToken).ConfigureAwait(false);
    }
}
