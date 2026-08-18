namespace FoodDiary.Integrations.Options;

public sealed class OpenFoodFactsApiOptions {
    public const string SectionName = "OpenFoodFacts";

    public string BaseUrl { get; init; } = "https://world.openfoodfacts.org";

    public string UserAgent { get; init; } = "FoodDiary/1.0";

    public static bool HasValidBaseUrl(OpenFoodFactsApiOptions options) =>
        Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out Uri? uri) &&
        string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    public static bool HasValidUserAgent(OpenFoodFactsApiOptions options) {
        using var request = new HttpRequestMessage();
        return request.Headers.UserAgent.TryParseAdd(options.UserAgent);
    }
}
