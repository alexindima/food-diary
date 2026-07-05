using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Queries.GetProfileOverview;

public sealed class GetProfileOverviewQueryHandler(IProfileOverviewReadService readService)
    : IQueryHandler<GetProfileOverviewQuery, Result<ProfileOverviewModel>> {
    public async Task<Result<ProfileOverviewModel>> Handle(GetProfileOverviewQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<ProfileOverviewModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);
        return await readService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
