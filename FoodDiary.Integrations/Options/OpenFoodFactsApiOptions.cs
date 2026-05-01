namespace FoodDiary.Integrations.Options;

public sealed class OpenFoodFactsApiOptions {
    public const string SectionName = "OpenFoodFacts";

    public string BaseUrl { get; init; } = "https://world.openfoodfacts.org";

    public string UserAgent { get; init; } = "FoodDiary/1.0";
}
