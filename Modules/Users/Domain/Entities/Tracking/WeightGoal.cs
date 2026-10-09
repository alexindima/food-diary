using FoodDiary.Modules.Users.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Domain.Entities.Tracking;

public sealed class WeightGoal : Entity<WeightGoalId> {
    public UserId UserId { get; private set; }
    public double TargetWeightKg { get; private set; }
    public double StartWeightKg { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public WeightGoalStatus Status { get; private set; }
    public DateTime? EndedAtUtc { get; private set; }
    public double? EndWeightKg { get; private set; }

    private WeightGoal() {
    }

    public static WeightGoal Start(UserId userId, double targetWeight, double startWeight, DateTime startedAtUtc) {
        var target = DesiredWeightKg.Create(targetWeight);
        var start = MeasuredWeightKg.FromGoalValue(startWeight);
        return StartWithMeasurements(userId, target, start, startedAtUtc);
    }

    public static WeightGoal StartWithMeasurements(UserId userId, DesiredWeightKg targetWeight, MeasuredWeightKg startWeight, DateTime startedAtUtc) {
        ArgumentNullException.ThrowIfNull(targetWeight);
        ArgumentNullException.ThrowIfNull(startWeight);
        if (userId.Value == Guid.Empty) {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        var goal = new WeightGoal {
            Id = WeightGoalId.New(),
            UserId = userId,
            TargetWeightKg = targetWeight.Value,
            StartWeightKg = startWeight.Value,
            StartedAtUtc = NormalizeUtc(startedAtUtc),
            Status = WeightGoalStatus.Active,
        };
        goal.SetCreated();
        return goal;
    }

    public void Replace(DateTime endedAtUtc, double endWeight) => End(WeightGoalStatus.Replaced, endedAtUtc, endWeight);

    public void Cancel(DateTime endedAtUtc, double endWeight) => End(WeightGoalStatus.Cancelled, endedAtUtc, endWeight);

    public void ReplaceWithMeasurement(DateTime endedAtUtc, MeasuredWeightKg endWeight) {
        ArgumentNullException.ThrowIfNull(endWeight);
        Replace(endedAtUtc, endWeight.Value);
    }

    public void CancelWithMeasurement(DateTime endedAtUtc, MeasuredWeightKg endWeight) {
        ArgumentNullException.ThrowIfNull(endWeight);
        Cancel(endedAtUtc, endWeight.Value);
    }

    private void End(WeightGoalStatus status, DateTime endedAtUtc, double endWeight) {
        if (Status != WeightGoalStatus.Active) {
            throw new InvalidOperationException("Only an active weight goal can be ended.");
        }

        DateTime normalized = NormalizeUtc(endedAtUtc);
        _ = DesiredWeightKg.Create(endWeight);
        if (normalized < StartedAtUtc) {
            throw new ArgumentOutOfRangeException(nameof(endedAtUtc));
        }

        Status = status;
        EndedAtUtc = normalized;
        EndWeightKg = endWeight;
        SetModified(normalized);
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? throw new ArgumentOutOfRangeException(nameof(value), "UTC timestamp kind must be specified.")
            : value.ToUniversalTime();
}
