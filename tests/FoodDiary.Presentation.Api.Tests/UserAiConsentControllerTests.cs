using FoodDiary.Results;
using FoodDiary.Application.Users.Commands.AcceptAiConsent;
using FoodDiary.Application.Users.Commands.RevokeAiConsent;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserAiConsentControllerTests {
    [Fact]
    public async Task AcceptAiConsent_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        UserAiConsentController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.AcceptAiConsent(userId);

        Assert.IsType<NoContentResult>(result);
        AcceptAiConsentCommand command = Assert.IsType<AcceptAiConsentCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
    }

    [Fact]
    public async Task RevokeAiConsent_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        UserAiConsentController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.RevokeAiConsent(userId);

        Assert.IsType<NoContentResult>(result);
        RevokeAiConsentCommand command = Assert.IsType<RevokeAiConsentCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
    }

    private static UserAiConsentController CreateController(ISender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
}
