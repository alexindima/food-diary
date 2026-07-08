using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetMyDietologist;

public sealed class GetMyDietologistQueryHandler(
    IDietologistInvitationReadService readService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetMyDietologistQuery, Result<DietologistInfoModel?>> {
    public async Task<Result<DietologistInfoModel?>> Handle(GetMyDietologistQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<DietologistInfoModel?>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        return await readService.GetMyDietologistAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
