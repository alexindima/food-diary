namespace FoodDiary.Web.Api.Options;

public sealed class PrivacyOptions {
    public const string SectionName = "Privacy";
    public bool RequireHealthConsent { get; init; } = true;
}
