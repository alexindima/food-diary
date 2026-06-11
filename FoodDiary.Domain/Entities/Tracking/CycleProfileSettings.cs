using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.Entities.Tracking;

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
    bool ClearNotes = false);
