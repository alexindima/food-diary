using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetClientGoals;

public sealed class GetClientGoalsQueryHandler(
    IDietologistClientReadService readService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetClientGoalsQuery, Result<UserModel>> {
    public async Task<Result<UserModel>> Handle(
        GetClientGoalsQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<UserModel>(userIdResult);
        }

        UserId dietologistUserId = userIdResult.Value;
        return await readService.GetGoalsAsync(dietologistUserId, query.ClientUserId, cancellationToken).ConfigureAwait(false);
    }
}
