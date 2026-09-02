using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class WaistGoal : Entity<WaistGoalId> {
    public UserId UserId { get; private set; }
    public double TargetWaistCm { get; private set; }
    public double StartWaistCm { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public WaistGoalStatus Status { get; private set; }
    public DateTime? EndedAtUtc { get; private set; }
    public double? EndWaistCm { get; private set; }

    private WaistGoal() {
    }

    public static WaistGoal Start(UserId userId, double targetWaist, double startWaist, DateTime startedAtUtc) {
        _ = DesiredWaistCm.Create(targetWaist);
        _ = DesiredWaistCm.Create(startWaist);
        if (userId.Value == Guid.Empty) {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        var goal = new WaistGoal {
            Id = WaistGoalId.New(),
            UserId = userId,
            TargetWaistCm = targetWaist,
            StartWaistCm = startWaist,
            StartedAtUtc = NormalizeUtc(startedAtUtc),
            Status = WaistGoalStatus.Active,
        };
        goal.SetCreated();
        return goal;
    }

    public void Replace(DateTime endedAtUtc, double endWaist) => End(WaistGoalStatus.Replaced, endedAtUtc, endWaist);

    public void Cancel(DateTime endedAtUtc, double endWaist) => End(WaistGoalStatus.Cancelled, endedAtUtc, endWaist);

    private void End(WaistGoalStatus status, DateTime endedAtUtc, double endWaist) {
        if (Status != WaistGoalStatus.Active) {
            throw new InvalidOperationException("Only an active waist goal can be ended.");
        }

        DateTime normalized = NormalizeUtc(endedAtUtc);
        _ = DesiredWaistCm.Create(endWaist);
        if (normalized < StartedAtUtc) {
            throw new ArgumentOutOfRangeException(nameof(endedAtUtc));
        }

        Status = status;
        EndedAtUtc = normalized;
        EndWaistCm = endWaist;
        SetModified(normalized);
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? throw new ArgumentOutOfRangeException(nameof(value), "UTC timestamp kind must be specified.")
            : value.ToUniversalTime();
}
