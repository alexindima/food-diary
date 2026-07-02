using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Services;

public sealed class HydrationGoalService(IUserRepository userRepository) : IHydrationGoalService {
    public async Task<Result<double?>> GetCurrentGoalAsync(UserId userId, CancellationToken cancellationToken = default) {
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<double?>(accessError);
        }

        return Result.Success(user!.HydrationGoal ?? user.WaterGoal);
    }
}
