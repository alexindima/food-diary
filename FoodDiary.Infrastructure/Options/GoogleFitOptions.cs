namespace FoodDiary.Infrastructure.Options;

public sealed class GoogleFitOptions {
    public const string SectionName = "GoogleFit";

    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;
}
