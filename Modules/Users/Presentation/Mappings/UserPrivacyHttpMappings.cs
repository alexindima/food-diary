using FoodDiary.Modules.Users.Application.Commands.UpdatePrivacyConsent;
using FoodDiary.Modules.Users.Application.Queries.GetUserPrivacy;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Presentation.Contracts.Responses;
using FoodDiary.Modules.Users.Presentation.Requests;

namespace FoodDiary.Modules.Users.Presentation.Mappings;

internal static class UserPrivacyHttpMappings {
    public static GetUserPrivacyQuery ToPrivacyQuery(this Guid userId) => new(userId);

    public static UpdatePrivacyConsentCommand ToPrivacyCommand(this UpdatePrivacyConsentHttpRequest request, Guid userId) =>
        new(userId, request.Purpose, request.Granted, request.NoticeVersion);

    public static UserPrivacyHttpResponse ToHttpResponse(this UserPrivacyStateModel value) =>
        new(value.NoticeVersion, value.HealthAcceptedAtUtc, value.AiAcceptedAtUtc, value.MarketingAcceptedAtUtc, value.HealthDeclinedAtUtc);
}
