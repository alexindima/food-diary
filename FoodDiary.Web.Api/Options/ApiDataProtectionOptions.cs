namespace FoodDiary.Web.Api.Options;

public sealed class ApiDataProtectionOptions {
    public const string SectionName = "DataProtection";

    public string ApplicationName { get; init; } = "FoodDiary.Web.Api";

    public string? KeyRingPath { get; init; }

    public static bool HasValidApplicationName(ApiDataProtectionOptions options) =>
        !string.IsNullOrWhiteSpace(options.ApplicationName);
}
