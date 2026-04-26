namespace FoodDiary.Integrations.Options;

public sealed class FitbitOptions {
    public const string SectionName = "Fitbit";

    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;
}
