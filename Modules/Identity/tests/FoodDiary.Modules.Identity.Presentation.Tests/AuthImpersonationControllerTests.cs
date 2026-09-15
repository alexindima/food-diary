using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Modules.Admin.Contracts.Commands.ExchangeAdminImpersonation;
using FoodDiary.Mediator;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Controllers;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Identity.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class AuthImpersonationControllerTests {
    [Fact]
    public async Task ExchangeImpersonation_ForwardsCodeAndReturnsAccessToken() {
        IRequest<Result<string>>? sent = null;
        ISender sender = SubstituteSender.Create(Result.Success("impersonation-token"), request => sent = request);
        var controller = new AuthImpersonationController(sender) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        IActionResult result = await controller.ExchangeImpersonation(new ExchangeImpersonationHttpRequest("single-use-code"));

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("impersonation-token", Assert.IsType<ExchangeImpersonationHttpResponse>(ok.Value).AccessToken);
        Assert.Equal("single-use-code", Assert.IsType<ExchangeAdminImpersonationCommand>(sent).Code);
    }
}
