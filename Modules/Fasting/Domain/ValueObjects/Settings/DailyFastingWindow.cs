using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Modules.Fasting.Domain.ValueObjects.Settings;

public sealed record DailyFastingWindow {
    public int FastHours { get; }
    public int EatingWindowHours { get; }

    private DailyFastingWindow(int fastHours, int eatingWindowHours) {
        FastHours = fastHours;
        EatingWindowHours = eatingWindowHours;
    }

    public static DailyFastingWindow FromHours(int fastHours, int eatingWindowHours) {
        FastingPlan.EnsureIntermittentHours(fastHours, eatingWindowHours);
        return new DailyFastingWindow(fastHours, eatingWindowHours);
    }
}
