using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User {
    public void UpdateAiTokenLimits(UserAiTokenLimitUpdate update) {
        EnsureNotDeleted();
        if (ApplyAiTokenLimitChanges(update)) {
            SetModified();
        }
    }

    public void UpdateGoals(
        double? dailyCalorieTarget = null,
        double? proteinTarget = null,
        double? fatTarget = null,
        double? carbTarget = null,
        double? fiberTarget = null,
        double? waterGoal = null,
        double? desiredWeight = null,
        double? desiredWaist = null) {
        UpdateGoals(new UserGoalUpdate(
            DailyCalorieTarget: dailyCalorieTarget,
            ProteinTarget: proteinTarget,
            FatTarget: fatTarget,
            CarbTarget: carbTarget,
            FiberTarget: fiberTarget,
            WaterGoal: waterGoal,
            DesiredWeight: desiredWeight,
            DesiredWaist: desiredWaist));
    }

    public void UpdateGoals(UserGoalUpdate update) {
        EnsureNotDeleted();
        var updatedGoals = GetNutritionGoals().With(
            dailyCalorieTarget: update.DailyCalorieTarget,
            proteinTarget: update.ProteinTarget,
            fatTarget: update.FatTarget,
            carbTarget: update.CarbTarget,
            fiberTarget: update.FiberTarget,
            waterGoal: update.WaterGoal);

        EnsureDesiredWeight(update.DesiredWeight, nameof(update.DesiredWeight));
        EnsureDesiredWaist(update.DesiredWaist, nameof(update.DesiredWaist));

        var state = GetGoalState() with {
            DailyCalorieTarget = updatedGoals.DailyCalorieTarget,
            ProteinTarget = updatedGoals.ProteinTarget,
            FatTarget = updatedGoals.FatTarget,
            CarbTarget = updatedGoals.CarbTarget,
            FiberTarget = updatedGoals.FiberTarget,
            WaterGoal = updatedGoals.WaterGoal,
            DesiredWeight = update.DesiredWeight.HasValue ? update.DesiredWeight : DesiredWeight,
            DesiredWaist = update.DesiredWaist.HasValue ? update.DesiredWaist : DesiredWaist
        };

        ApplyGoalState(state);

        SetModified();
    }

    public void UpdateAiTokenLimits(long? inputLimit, long? outputLimit) {
        EnsureNotDeleted();
        if (ApplyAiTokenLimitChanges(new UserAiTokenLimitUpdate(inputLimit, outputLimit))) {
            SetModified();
        }
    }

    public void UpdateDesiredWeight(double? desiredWeight) {
        EnsureNotDeleted();
        EnsureDesiredWeight(desiredWeight, nameof(desiredWeight));
        ApplyGoalState(GetGoalState() with { DesiredWeight = desiredWeight });
        SetModified();
    }

    public void UpdateDesiredWaist(double? desiredWaist) {
        EnsureNotDeleted();
        EnsureDesiredWaist(desiredWaist, nameof(desiredWaist));
        ApplyGoalState(GetGoalState() with { DesiredWaist = desiredWaist });
        SetModified();
    }

    private bool ApplyAiTokenLimitChanges(UserAiTokenLimitUpdate update) {
        var currentState = GetAccountState();
        var nextState = currentState.WithAiTokenLimits(update.InputLimit, update.OutputLimit);
        if (nextState == currentState) {
            return false;
        }

        ApplyAccountState(nextState);
        return true;
    }
}
