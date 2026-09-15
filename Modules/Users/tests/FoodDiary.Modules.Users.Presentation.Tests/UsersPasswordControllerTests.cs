using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Application.Commands.ChangePassword;
using FoodDiary.Modules.Users.Application.Commands.SetPassword;
using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Presentation.Controllers;
using FoodDiary.Modules.Users.Presentation.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Users.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class UsersPasswordControllerTests {
    [Fact]
    public async Task ChangePassword_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        UsersPasswordController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var request = new ChangePasswordHttpRequest("old-password", "new-password");

        IActionResult result = await controller.ChangePassword(userId, request);

        Assert.IsType<NoContentResult>(result);
        ChangePasswordCommand command = Assert.IsType<ChangePasswordCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("old-password", command.CurrentPassword);
        Assert.Equal("new-password", command.NewPassword);
    }

    [Fact]
    public async Task SetPassword_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        UsersPasswordController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var request = new SetPasswordHttpRequest("new-password");

        IActionResult result = await controller.SetPassword(userId, request);

        Assert.IsType<NoContentResult>(result);
        SetPasswordCommand command = Assert.IsType<SetPasswordCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("new-password", command.NewPassword);
    }

    private static UsersPasswordController CreateController(ISender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
}
