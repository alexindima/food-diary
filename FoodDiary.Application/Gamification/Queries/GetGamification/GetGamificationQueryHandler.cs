using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Gamification.Queries.GetGamification;

public sealed class GetGamificationQueryHandler(IGamificationReadService readService)
    : IQueryHandler<GetGamificationQuery, Result<GamificationModel>> {
    public async Task<Result<GamificationModel>> Handle(
        GetGamificationQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<GamificationModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        return await readService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
