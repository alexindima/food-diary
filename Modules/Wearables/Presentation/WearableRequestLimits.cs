using FoodDiary.Modules.Wearables.Application.Abstractions.Common;

namespace FoodDiary.Modules.Wearables.Presentation;

public static class WearableRequestLimits {
    public const int MaximumProviderLength = WearableInputLimits.MaximumProviderLength;
    public const int MaximumOAuthStateLength = WearableInputLimits.MaximumOAuthStateLength;
    public const int MaximumAuthorizationCodeLength = WearableInputLimits.MaximumAuthorizationCodeLength;
    public const int MaximumProtectedOAuthStateLength = WearableInputLimits.MaximumProtectedOAuthStateLength;
}
