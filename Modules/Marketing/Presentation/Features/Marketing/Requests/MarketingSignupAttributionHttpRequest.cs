using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FoodDiary.Presentation.Api.Features.Marketing.Requests;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record MarketingSignupAttributionHttpRequest(
    string Timestamp,
    string AnonymousId,
    string SessionId,
    string LandingPath,
    string? ReferrerHost = null,
    string? UtmSource = null,
    string? UtmMedium = null,
    string? UtmCampaign = null,
    string? UtmContent = null,
    string? UtmTerm = null,
    string? BuildVersion = null) : IValidatableObject {
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        MarketingAttributionHttpRequestValidation.Validate(
            Timestamp,
            AnonymousId,
            SessionId,
            LandingPath,
            ReferrerHost,
            UtmSource,
            UtmMedium,
            UtmCampaign,
            UtmContent,
            UtmTerm,
            BuildVersion);
}
