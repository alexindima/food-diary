using System.Net;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.Privacy;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
internal static class PrivacyConsentScenario {
    public static async Task GrantHealthTrackingConsentAsync(this HttpClient client, string? browserSessionCookie = null,
        CancellationToken cancellationToken = default) {
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/users/privacy") {
            Content = JsonContent.Create(new {
                Purpose = PrivacyConsentPurpose.HealthTracking,
                Granted = true,
                NoticeVersion = PrivacyNoticeVersions.Current,
            }),
        };
        if (browserSessionCookie is not null) { request.Headers.Add("Cookie", browserSessionCookie); }
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
