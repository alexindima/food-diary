using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Cycles.Domain.Entities;

public sealed record CycleProfileSettings(
    CycleTrackingMode Mode,
    int? AverageCycleLength,
    int? AveragePeriodLength,
    int? LutealLength,
    bool? IsRegular,
    bool? IsOnboardingComplete,
    bool? ShowFertilityEstimates,
    bool? DiscreetNotifications,
    string? Notes,
    bool ClearNotes = false,
    CycleTrackingGoal? Goal = null,
    CycleReproductiveState? ReproductiveState = null,
    bool? HideFromDashboard = null);
