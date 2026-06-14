using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Users.Commands.ChangePassword;
using FoodDiary.Application.Users.Commands.SetPassword;
using FoodDiary.Presentation.Api.Features.Users;
using FoodDiary.Presentation.Api.Features.Users.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class UsersPasswordControllerTests {
    [Fact]
    public async Task ChangePassword_SendsCommandAndReturnsNoContent() {
        RecordingSender sender = new(Result.Success());
        UsersPasswordController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var request = new ChangePasswordHttpRequest("old-password", "new-password");

        IActionResult result = await controller.ChangePassword(userId, request);

        Assert.IsType<NoContentResult>(result);
        ChangePasswordCommand command = Assert.IsType<ChangePasswordCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("old-password", command.CurrentPassword);
        Assert.Equal("new-password", command.NewPassword);
    }

    [Fact]
    public async Task SetPassword_SendsCommandAndReturnsNoContent() {
        RecordingSender sender = new(Result.Success());
        UsersPasswordController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var request = new SetPasswordHttpRequest("new-password");

        IActionResult result = await controller.SetPassword(userId, request);

        Assert.IsType<NoContentResult>(result);
        SetPasswordCommand command = Assert.IsType<SetPasswordCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("new-password", command.NewPassword);
    }

    private static UsersPasswordController CreateController(RecordingSender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
}
