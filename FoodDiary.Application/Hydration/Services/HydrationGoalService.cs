using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Services;

public sealed class HydrationGoalService(IUserContextService userContextService) : IHydrationGoalService {
    public async Task<Result<double?>> GetCurrentGoalAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<double?>(userResult.Error);
        }

        User user = userResult.Value;
        return Result.Success(user.HydrationGoal ?? user.WaterGoal);
    }
}
