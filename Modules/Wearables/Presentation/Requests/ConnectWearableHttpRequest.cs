using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Wearables.Presentation.Requests;

public sealed record ConnectWearableHttpRequest(string Code, string State) {
    public ConnectWearableHttpRequest() : this(string.Empty, string.Empty) { }

    [Required, MaxLength(WearableRequestLimits.MaximumAuthorizationCodeLength)]
    public string Code { get; init; } = Code;

    [Required, MaxLength(WearableRequestLimits.MaximumProtectedOAuthStateLength)]
    public string State { get; init; } = State;
}
