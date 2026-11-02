using FoodDiary.Modules.Users.Application.Queries.GetUserPrivacy;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Presentation.Contracts.Responses;
using FoodDiary.Modules.Users.Presentation.Controllers;
using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Users.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserPrivacyControllerTests {
    [Fact]
    public async Task Read_UsesTheAuthenticatedUserAndReturnsAllConsentTimestamps() {
        DateTime timestamp = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
        var state = new UserPrivacyStateModel("current-notice", timestamp, timestamp.AddDays(1), timestamp.AddDays(2), timestamp.AddDays(3));
        CapturedSender sender = SubstituteSender.Capture(Result.Success(state));
        var controller = new UserPrivacyController(sender) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        var id = Guid.NewGuid();

        UserPrivacyHttpResponse response = Assert.IsType<UserPrivacyHttpResponse>(Assert.IsType<OkObjectResult>(await controller.GetPrivacy(id)).Value);

        Assert.Multiple(
            () => Assert.Equal(id, Assert.IsType<GetUserPrivacyQuery>(sender.Request).UserId),
            () => Assert.Equal(state.NoticeVersion, response.NoticeVersion),
            () => Assert.Equal(state.HealthAcceptedAtUtc, response.HealthAcceptedAtUtc),
            () => Assert.Equal(state.AiAcceptedAtUtc, response.AiAcceptedAtUtc),
            () => Assert.Equal(state.MarketingAcceptedAtUtc, response.MarketingAcceptedAtUtc),
            () => Assert.Equal(state.HealthDeclinedAtUtc, response.HealthDeclinedAtUtc));
    }
}
