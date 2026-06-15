namespace FoodDiary.Web.Api.Options;

public sealed class ApiHttpsRedirectionOptions {
    public const string SectionName = "HttpsRedirection";

    public bool Enabled { get; init; }
}
