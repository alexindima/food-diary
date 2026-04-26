namespace FoodDiary.Integrations.Options;

public sealed class UsdaApiOptions {
    public const string SectionName = "UsdaApi";

    public string ApiKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://api.nal.usda.gov/fdc/v1";
}
