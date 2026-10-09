using FoodDiary.Modules.Fasting.Domain.Enums;

namespace FoodDiary.Modules.Fasting.Domain.ValueObjects.Settings;

public abstract class FastingPlanSettings {
    private protected FastingPlanSettings() {
    }

    public abstract FastingPlanType Type { get; }
}
