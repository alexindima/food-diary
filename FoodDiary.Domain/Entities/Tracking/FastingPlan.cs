using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class FastingPlan : AggregateRoot<FastingPlanId> {
    private const int TitleMaxLength = 120;
    private const int MinIntermittentHours = 1;
    private const int MaxExtendedHours = 168;
    private const int MaxCycleDays = 30;

    public UserId UserId { get; private set; }
    public FastingPlanType Type { get; private set; }
    public FastingPlanStatus Status { get; private set; }
    public FastingProtocol? Protocol { get; private set; }
    public string? Title { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? StoppedAtUtc { get; private set; }

    public int? IntermittentFastHours { get; private set; }
    public int? IntermittentEatingWindowHours { get; private set; }

    public int? ExtendedTargetHours { get; private set; }

    public int? CyclicFastDays { get; private set; }
    public int? CyclicEatDays { get; private set; }
    public int? CyclicEatDayFastHours { get; private set; }
    public int? CyclicEatDayEatingWindowHours { get; private set; }
    public DateTime? CyclicAnchorDateUtc { get; private set; }
    public DateTime? CyclicNextPhaseDateUtc { get; private set; }

    public User User { get; private set; } = null!;

    private FastingPlan() {
    }

    public static FastingPlan CreateIntermittent(
        UserId userId,
        FastingProtocol protocol,
        int fastHours,
        int eatingWindowHours,
        DateTime startedAtUtc,
        string? title = null) {
        EnsureUserId(userId);
        EnsureIntermittentProtocol(protocol);
        EnsureIntermittentHours(fastHours, eatingWindowHours);

        var plan = new FastingPlan {
            Id = FastingPlanId.New(),
            UserId = userId,
            Type = FastingPlanType.Intermittent,
            Status = FastingPlanStatus.Active,
            Protocol = protocol,
            Title = NormalizeTitle(title),
            StartedAtUtc = NormalizeTimestamp(startedAtUtc, nameof(startedAtUtc)),
            IntermittentFastHours = fastHours,
            IntermittentEatingWindowHours = eatingWindowHours
        };

        plan.SetCreated();
        return plan;
    }

    public static FastingPlan CreateExtended(
        UserId userId,
        FastingProtocol protocol,
        int targetHours,
        DateTime startedAtUtc,
        string? title = null) {
        EnsureUserId(userId);
        EnsureExtendedProtocol(protocol);
        EnsureExtendedTargetHours(targetHours);

        var plan = new FastingPlan {
            Id = FastingPlanId.New(),
            UserId = userId,
            Type = FastingPlanType.Extended,
            Status = FastingPlanStatus.Active,
            Protocol = protocol,
            Title = NormalizeTitle(title),
            StartedAtUtc = NormalizeTimestamp(startedAtUtc, nameof(startedAtUtc)),
            ExtendedTargetHours = targetHours
        };

        plan.SetCreated();
        return plan;
    }

    public static FastingPlan CreateCyclic(
        UserId userId,
        int fastDays,
        int eatDays,
        int eatDayFastHours,
        int eatDayEatingWindowHours,
        DateTime anchorDateUtc,
        DateTime startedAtUtc,
        string? title = null) {
        EnsureUserId(userId);
        EnsureCyclicDays(fastDays, eatDays);
        EnsureIntermittentHours(eatDayFastHours, eatDayEatingWindowHours);

        var normalizedAnchorDate = NormalizeDate(anchorDateUtc, nameof(anchorDateUtc));

        var plan = new FastingPlan {
            Id = FastingPlanId.New(),
            UserId = userId,
            Type = FastingPlanType.Cyclic,
            Status = FastingPlanStatus.Active,
            Title = NormalizeTitle(title),
            StartedAtUtc = NormalizeTimestamp(startedAtUtc, nameof(startedAtUtc)),
            CyclicFastDays = fastDays,
            CyclicEatDays = eatDays,
            CyclicEatDayFastHours = eatDayFastHours,
            CyclicEatDayEatingWindowHours = eatDayEatingWindowHours,
            CyclicAnchorDateUtc = normalizedAnchorDate,
            CyclicNextPhaseDateUtc = normalizedAnchorDate
        };

        plan.SetCreated();
        return plan;
    }

    public void Pause() {
        if (Status != FastingPlanStatus.Active) {
            return;
        }

        Status = FastingPlanStatus.Paused;
        SetModified();
    }

    public void Resume() {
        if (Status != FastingPlanStatus.Paused) {
            return;
        }

        Status = FastingPlanStatus.Active;
        SetModified();
    }

    public void Stop(DateTime stoppedAtUtc) {
        if (Status == FastingPlanStatus.Stopped) {
            return;
        }

        Status = FastingPlanStatus.Stopped;
        StoppedAtUtc = NormalizeTimestamp(stoppedAtUtc, nameof(stoppedAtUtc));
        SetModified();
    }

    public void Rename(string? title) {
        var normalizedTitle = NormalizeTitle(title);
        if (Title == normalizedTitle) {
            return;
        }

        Title = normalizedTitle;
        SetModified();
    }

    public void ScheduleNextCyclicPhase(DateTime nextPhaseDateUtc) {
        if (Type != FastingPlanType.Cyclic) {
            throw new InvalidOperationException("Only cyclic fasting plans can change the next phase date.");
        }

        var normalizedDate = NormalizeDate(nextPhaseDateUtc, nameof(nextPhaseDateUtc));
        if (CyclicNextPhaseDateUtc == normalizedDate) {
            return;
        }

        CyclicNextPhaseDateUtc = normalizedDate;
        SetModified();
    }

    private static DateTime NormalizeTimestamp(DateTime value, string paramName) {
        if (value.Kind == DateTimeKind.Unspecified) {
            throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.");
        }

        return value.ToUniversalTime();
    }

    private static DateTime NormalizeDate(DateTime value, string paramName) {
        var utc = NormalizeTimestamp(value, paramName);
        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }

    private static string? NormalizeTitle(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > TitleMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Title must be at most {TitleMaxLength} characters.")
            : trimmed;
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }

    private static void EnsureIntermittentProtocol(FastingProtocol protocol) {
        if (protocol is not (FastingProtocol.F16_8 or FastingProtocol.F18_6 or FastingProtocol.F20_4 or FastingProtocol.CustomIntermittent)) {
            throw new ArgumentOutOfRangeException(nameof(protocol), "Protocol is not valid for intermittent fasting.");
        }
    }

    private static void EnsureExtendedProtocol(FastingProtocol protocol) {
        if (protocol is not (FastingProtocol.F24_0 or FastingProtocol.F36_0 or FastingProtocol.F72_0 or FastingProtocol.Custom)) {
            throw new ArgumentOutOfRangeException(nameof(protocol), "Protocol is not valid for extended fasting.");
        }
    }

    private static void EnsureIntermittentHours(int fastHours, int eatingWindowHours) {
        if (fastHours is < MinIntermittentHours or > 23) {
            throw new ArgumentOutOfRangeException(nameof(fastHours), "Fast hours must be in range [1, 23].");
        }

        if (eatingWindowHours is < MinIntermittentHours or > 23) {
            throw new ArgumentOutOfRangeException(nameof(eatingWindowHours), "Eating window hours must be in range [1, 23].");
        }

        if (fastHours + eatingWindowHours != 24) {
            throw new ArgumentOutOfRangeException(nameof(eatingWindowHours), "Intermittent fasting windows must add up to 24 hours.");
        }
    }

    private static void EnsureExtendedTargetHours(int targetHours) {
        if (targetHours < MinIntermittentHours || targetHours > MaxExtendedHours) {
            throw new ArgumentOutOfRangeException(nameof(targetHours), $"Extended fasting target must be between {MinIntermittentHours} and {MaxExtendedHours} hours.");
        }
    }

    private static void EnsureCyclicDays(int fastDays, int eatDays) {
        if (fastDays is < 1 or > MaxCycleDays) {
            throw new ArgumentOutOfRangeException(nameof(fastDays), $"Fast days must be in range [1, {MaxCycleDays}].");
        }

        if (eatDays is < 1 or > MaxCycleDays) {
            throw new ArgumentOutOfRangeException(nameof(eatDays), $"Eat days must be in range [1, {MaxCycleDays}].");
        }
    }
}
