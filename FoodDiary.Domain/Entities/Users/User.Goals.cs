using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;

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
            DesiredWeightKg: desiredWeight,
            DesiredWaistCm: desiredWaist));
    }

    public void UpdateGoals(UserGoalUpdate update) {
        EnsureNotDeleted();
        UserNutritionGoals updatedGoals = GetNutritionGoals().With(
            dailyCalorieTarget: update.DailyCalorieTarget,
            proteinTarget: update.ProteinTarget,
            fatTarget: update.FatTarget,
            carbTarget: update.CarbTarget,
            fiberTarget: update.FiberTarget,
            waterGoal: update.WaterGoal);

        EnsureDesiredWeight(update.DesiredWeightKg, nameof(update.DesiredWeightKg));
        EnsureDesiredWaist(update.DesiredWaistCm, nameof(update.DesiredWaistCm));
        EnsureDayCalorieTarget(update.MondayCalories, nameof(update.MondayCalories));
        EnsureDayCalorieTarget(update.TuesdayCalories, nameof(update.TuesdayCalories));
        EnsureDayCalorieTarget(update.WednesdayCalories, nameof(update.WednesdayCalories));
        EnsureDayCalorieTarget(update.ThursdayCalories, nameof(update.ThursdayCalories));
        EnsureDayCalorieTarget(update.FridayCalories, nameof(update.FridayCalories));
        EnsureDayCalorieTarget(update.SaturdayCalories, nameof(update.SaturdayCalories));
        EnsureDayCalorieTarget(update.SundayCalories, nameof(update.SundayCalories));

        UserGoalState state = new(
            DailyCalorieTarget: updatedGoals.DailyCalorieTarget,
            ProteinTarget: updatedGoals.ProteinTarget,
            FatTarget: updatedGoals.FatTarget,
            CarbTarget: updatedGoals.CarbTarget,
            FiberTarget: updatedGoals.FiberTarget,
            WaterGoal: updatedGoals.WaterGoal,
            DesiredWeightKg: update.DesiredWeightKg ?? DesiredWeightKg,
            DesiredWaistCm: update.DesiredWaistCm ?? DesiredWaistCm,
            CalorieCyclingEnabled: update.CalorieCyclingEnabled ?? CalorieCyclingEnabled,
            MondayCalories: update.MondayCalories ?? MondayCalories,
            TuesdayCalories: update.TuesdayCalories ?? TuesdayCalories,
            WednesdayCalories: update.WednesdayCalories ?? WednesdayCalories,
            ThursdayCalories: update.ThursdayCalories ?? ThursdayCalories,
            FridayCalories: update.FridayCalories ?? FridayCalories,
            SaturdayCalories: update.SaturdayCalories ?? SaturdayCalories,
            SundayCalories: update.SundayCalories ?? SundayCalories);

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
        ApplyGoalState(GetGoalState() with { DesiredWeightKg = desiredWeight });
        SetModified();
    }

    public WeightGoal StartWeightGoal(double targetWeight, double startWeight, DateTime startedAtUtc) {
        EnsureNotDeleted();
        DateTime nowUtc = startedAtUtc.ToUniversalTime();
        WeightGoal? activeGoal = _weightGoals.SingleOrDefault(goal => goal.Status == WeightGoalStatus.Active);
        activeGoal?.Replace(nowUtc, startWeight);

        var goal = WeightGoal.Start(Id, targetWeight, startWeight, nowUtc);
        _weightGoals.Add(goal);
        UpdateDesiredWeight(targetWeight);
        return goal;
    }

    public void CancelWeightGoal(DateTime endedAtUtc, double endWeight) {
        EnsureNotDeleted();
        WeightGoal? activeGoal = _weightGoals.SingleOrDefault(goal => goal.Status == WeightGoalStatus.Active);
        activeGoal?.Cancel(endedAtUtc, endWeight);
        UpdateDesiredWeight(desiredWeight: null);
    }

    public void UpdateDesiredWaist(double? desiredWaist) {
        EnsureNotDeleted();
        EnsureDesiredWaist(desiredWaist, nameof(desiredWaist));
        ApplyGoalState(GetGoalState() with { DesiredWaistCm = desiredWaist });
        SetModified();
    }

    public WaistGoal StartWaistGoal(double targetWaist, double startWaist, DateTime startedAtUtc) {
        EnsureNotDeleted();
        DateTime nowUtc = startedAtUtc.ToUniversalTime();
        WaistGoal? activeGoal = _waistGoals.SingleOrDefault(goal => goal.Status == WaistGoalStatus.Active);
        activeGoal?.Replace(nowUtc, startWaist);

        var goal = WaistGoal.Start(Id, targetWaist, startWaist, nowUtc);
        _waistGoals.Add(goal);
        UpdateDesiredWaist(targetWaist);
        return goal;
    }

    public void CancelWaistGoal(DateTime endedAtUtc, double endWaist) {
        EnsureNotDeleted();
        WaistGoal? activeGoal = _waistGoals.SingleOrDefault(goal => goal.Status == WaistGoalStatus.Active);
        activeGoal?.Cancel(endedAtUtc, endWaist);
        UpdateDesiredWaist(desiredWaist: null);
    }

    private bool ApplyAiTokenLimitChanges(UserAiTokenLimitUpdate update) {
        UserAiQuotaState currentState = GetAiQuotaState();
        UserAiQuotaState nextState = currentState.WithLimits(update.InputLimit, update.OutputLimit);
        if (nextState == currentState) {
            return false;
        }

        ApplyAiQuotaState(nextState);
        return true;
    }
}
