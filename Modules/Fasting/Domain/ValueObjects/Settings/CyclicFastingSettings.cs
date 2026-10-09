using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Fasting.Domain.Enums;

namespace FoodDiary.Modules.Fasting.Domain.ValueObjects.Settings;

public sealed class CyclicFastingSettings : FastingPlanSettings {
    public override FastingPlanType Type => FastingPlanType.Cyclic;
    public int FastDays { get; }
    public int EatDays { get; }
    public DailyFastingWindow EatingDayWindow { get; }
    public FastingCycleDay AnchorDay { get; }

    private CyclicFastingSettings(int fastDays, int eatDays, DailyFastingWindow eatingDayWindow, FastingCycleDay anchorDay) {
        FastDays = fastDays;
        EatDays = eatDays;
        EatingDayWindow = eatingDayWindow;
        AnchorDay = anchorDay;
    }

    public static CyclicFastingSettings Create(int fastDays, int eatDays, DailyFastingWindow eatingDayWindow, FastingCycleDay anchorDay) {
        FastingPlan.EnsureCyclicDays(fastDays, eatDays);
        ArgumentNullException.ThrowIfNull(eatingDayWindow);
        return new CyclicFastingSettings(fastDays, eatDays, eatingDayWindow, anchorDay);
    }

    public static CyclicFastingSettings FromDateTimeEncoding(int fastDays, int eatDays, int eatDayFastHours, int eatDayEatingWindowHours, DateTime anchorDateUtc) {
        FastingPlan.EnsureCyclicDays(fastDays, eatDays);
        var window = DailyFastingWindow.FromHours(eatDayFastHours, eatDayEatingWindowHours);
        var day = FastingCycleDay.FromDateTimeEncoding(anchorDateUtc);
        return new CyclicFastingSettings(fastDays, eatDays, window, day);
    }
}
