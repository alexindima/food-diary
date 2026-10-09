using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Fasting.Domain.Enums;

namespace FoodDiary.Modules.Fasting.Domain.ValueObjects.Settings;

public sealed class IntermittentFastingSettings : FastingPlanSettings {
    public override FastingPlanType Type => FastingPlanType.Intermittent;
    public FastingProtocol Protocol { get; }
    public DailyFastingWindow Window { get; }

    private IntermittentFastingSettings(FastingProtocol protocol, DailyFastingWindow window) {
        Protocol = protocol;
        Window = window;
    }

    public static IntermittentFastingSettings Create(FastingProtocol protocol, int fastHours, int eatingWindowHours) {
        FastingPlan.EnsureIntermittentProtocol(protocol);
        return new IntermittentFastingSettings(protocol, DailyFastingWindow.FromHours(fastHours, eatingWindowHours));
    }
}
