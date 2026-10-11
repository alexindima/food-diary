using FoodDiary.Results;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Application.Common;
using FoodDiary.Modules.Users.Contracts.Commands.InitializePendingBodyGoals;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Enums;

namespace FoodDiary.Modules.Users.Application.Commands.InitializePendingBodyGoals;

public sealed class InitializePendingBodyGoalsCommandHandler(IUserContextService users, TimeProvider timeProvider)
    : IRequestHandler<InitializePendingBodyGoalsCommand, bool> {
    public async Task<bool> Handle(InitializePendingBodyGoalsCommand request, CancellationToken cancellationToken) {
        Result<User> result = await users.GetAccessibleUserAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure) {
            return false;
        }
        User user = result.Value;
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        bool changed = false;
        if (request.Weight is not null && user.DesiredWeightKg is { } weightTarget &&
            !user.WeightGoals.Any(goal => goal.Status == WeightGoalStatus.Active)) {
            user.StartWeightGoalWithMeasurements(DesiredWeightKg.Create(weightTarget), request.Weight, now);
            changed = true;
        }
        if (request.Waist is not null && user.DesiredWaistCm is { } waistTarget &&
            !user.WaistGoals.Any(goal => goal.Status == WaistGoalStatus.Active)) {
            user.StartWaistGoalWithMeasurements(DesiredWaistCm.Create(waistTarget), request.Waist, now);
            changed = true;
        }
        if (changed) {
            await users.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);
        }
        return changed;
    }
}
