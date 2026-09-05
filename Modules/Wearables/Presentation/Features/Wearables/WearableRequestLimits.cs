using FoodDiary.Application.Abstractions.Wearables.Common;

namespace FoodDiary.Presentation.Api.Features.Wearables;

public static class WearableRequestLimits {
    public const int MaximumProviderLength = WearableInputLimits.MaximumProviderLength;
    public const int MaximumOAuthStateLength = WearableInputLimits.MaximumOAuthStateLength;
    public const int MaximumAuthorizationCodeLength = WearableInputLimits.MaximumAuthorizationCodeLength;
    public const int MaximumProtectedOAuthStateLength = WearableInputLimits.MaximumProtectedOAuthStateLength;
}
