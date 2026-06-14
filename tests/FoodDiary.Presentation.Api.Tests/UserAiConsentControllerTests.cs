using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Users.Commands.AcceptAiConsent;
using FoodDiary.Application.Users.Commands.RevokeAiConsent;
using FoodDiary.Presentation.Api.Features.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserAiConsentControllerTests {
    [Fact]
    public async Task AcceptAiConsent_SendsCommandAndReturnsNoContent() {
        RecordingSender sender = new(Result.Success());
        UserAiConsentController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.AcceptAiConsent(userId);

        Assert.IsType<NoContentResult>(result);
        AcceptAiConsentCommand command = Assert.IsType<AcceptAiConsentCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
    }

    [Fact]
    public async Task RevokeAiConsent_SendsCommandAndReturnsNoContent() {
        RecordingSender sender = new(Result.Success());
        UserAiConsentController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.RevokeAiConsent(userId);

        Assert.IsType<NoContentResult>(result);
        RevokeAiConsentCommand command = Assert.IsType<RevokeAiConsentCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
    }

    private static UserAiConsentController CreateController(RecordingSender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
}
