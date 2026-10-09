using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Fasting.Domain.Enums;

namespace FoodDiary.Modules.Fasting.Domain.ValueObjects.Settings;

public sealed class ExtendedFastingSettings : FastingPlanSettings {
    public override FastingPlanType Type => FastingPlanType.Extended;
    public FastingProtocol Protocol { get; }
    public int TargetHours { get; }

    private ExtendedFastingSettings(FastingProtocol protocol, int targetHours) {
        Protocol = protocol;
        TargetHours = targetHours;
    }

    public static ExtendedFastingSettings Create(FastingProtocol protocol, int targetHours) {
        FastingPlan.EnsureExtendedProtocol(protocol);
        FastingPlan.EnsureExtendedTargetHours(targetHours);
        return new ExtendedFastingSettings(protocol, targetHours);
    }
}
