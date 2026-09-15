using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Wearables.Presentation.Requests;

public sealed record ConnectWearableHttpRequest(
    [property: Required, MaxLength(WearableRequestLimits.MaximumAuthorizationCodeLength)] string Code,
    [property: Required, MaxLength(WearableRequestLimits.MaximumProtectedOAuthStateLength)] string State);
