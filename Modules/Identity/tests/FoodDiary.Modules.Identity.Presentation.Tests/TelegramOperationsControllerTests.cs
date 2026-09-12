using System.Reflection;
using FoodDiary.Application.Identity.Authentication.Commands.RegisterTelegramOperation;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Auth;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Responses;
using FoodDiary.Presentation.Api.Security;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelegramOperationsControllerTests {
    [Fact]
    public void AllOperations_RequireBotSecretAndDisableResponseCaching() {
        Assert.NotNull(typeof(TelegramOperationsController).GetCustomAttribute<RequireTelegramBotSecretAttribute>());
        ResponseCacheAttribute? cache = typeof(TelegramOperationsController).GetCustomAttribute<ResponseCacheAttribute>();
        Assert.NotNull(cache);
        Assert.True(cache.NoStore);
    }

    [Fact]
    public async Task Register_UsesServerOwnedBotIdentityAndReturnsOperationId() {
        IRequest<Result<Guid>>? sent = null;
        var operationId = Guid.NewGuid();
        ISender sender = SubstituteSender.Create(Result.Success(operationId), request => sent = request);
        var controller = new TelegramOperationsController(sender) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        IActionResult result = await controller.Register(new RegisterTelegramOperationHttpRequest(77, 123, "payload"));

        RegisterTelegramOperationCommand command = Assert.IsType<RegisterTelegramOperationCommand>(sent);
        Assert.Equal(77, command.UpdateId);
        Assert.Equal(123, command.TelegramUserId);
        OkObjectResult response = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(operationId, Assert.IsType<TelegramOperationRegisteredHttpResponse>(response.Value).OperationId);
    }
}
