namespace FoodDiary.Web.Api.Options;

public sealed class ApiCorsOptions {
    public const string SectionName = "Cors";

    public string[] Origins { get; init; } = [];

    public static bool HasValidOrigins(ApiCorsOptions options) {
        return options.Origins.All(origin => Uri.TryCreate(origin, UriKind.Absolute, out _));
    }
}
