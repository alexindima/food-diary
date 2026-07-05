using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetInvitationByToken;

public sealed class GetInvitationByTokenQueryHandler(IDietologistInvitationReadService readService)
    : IQueryHandler<GetInvitationByTokenQuery, Result<InvitationModel>> {
    public async Task<Result<InvitationModel>> Handle(GetInvitationByTokenQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<InvitationModel>(userIdResult.Error);
        }

        return await readService.GetByTokenAsync(userIdResult.Value, query.InvitationId, cancellationToken).ConfigureAwait(false);
    }
}
