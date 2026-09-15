using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Modules.Identity.Application.Authentication.Queries.GetTelegramConfiguration;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.BeginTelegramMiniApp;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.StartTelegramOidc;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.ExchangeTelegramOidc;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Mediator;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Controllers;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Identity.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelegramConfigurationControllerTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StartOidc_BindsBrowserAndOptionalCurrentOwner(bool linking) {
        Guid? owner = linking ? Guid.NewGuid() : null;
        IRequest<Result<TelegramOidcStartModel>>? sent = null;
        ISender sender = SubstituteSender.Create(Result.Success(new TelegramOidcStartModel("https://oauth.telegram.org/auth")), request => sent = request);
        AuthTelegramOnboardingController controller = Create(sender);
        OkObjectResult result = Assert.IsType<OkObjectResult>(linking ? await controller.StartOidcLink(owner!.Value) : await controller.StartOidc());
        StartTelegramOidcCommand command = Assert.IsType<StartTelegramOidcCommand>(sent);
        Assert.Equal(owner, command.LinkUserId);
        Assert.Equal(new string('b', 43), command.BrowserBinding);
        Assert.Equal("https://oauth.telegram.org/auth", Assert.IsType<TelegramOidcStartHttpResponse>(result.Value).AuthorizationUrl);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BeginMiniApp_BindsProofToBrowserAndOptionalOwner(bool linking) {
        Guid? owner = linking ? Guid.NewGuid() : null;
        var intent = new TelegramAuthenticationIntentModel("ticket", "onboarding", DateTime.UtcNow.AddMinutes(1));
        IRequest<Result<TelegramAuthenticationIntentModel>>? sent = null;
        ISender sender = SubstituteSender.Create(Result.Success(intent), request => sent = request);
        AuthTelegramOnboardingController controller = Create(sender);
        var proof = new TelegramAuthHttpRequest("signed-proof");
        OkObjectResult result = Assert.IsType<OkObjectResult>(linking ? await controller.BeginLink(owner!.Value, proof) : await controller.Begin(proof));
        BeginTelegramMiniAppCommand command = Assert.IsType<BeginTelegramMiniAppCommand>(sent);
        Assert.Equal(owner, command.LinkUserId);
        Assert.Equal("signed-proof", command.InitData);
        Assert.Equal(new string('b', 43), command.BrowserBinding);
        Assert.Equal(intent.Ticket, Assert.IsType<TelegramAuthenticationIntentHttpResponse>(result.Value).Ticket);
    }

    [Fact]
    public async Task ExchangeOidc_UsesCodeStateAndExistingBrowserBinding() {
        var intent = new TelegramAuthenticationIntentModel("ticket", "login", DateTime.UtcNow.AddMinutes(1));
        IRequest<Result<TelegramAuthenticationIntentModel>>? sent = null;
        ISender sender = SubstituteSender.Create(Result.Success(intent), request => sent = request);
        OkObjectResult result = Assert.IsType<OkObjectResult>(await Create(sender).ExchangeOidc(new ExchangeTelegramOidcHttpRequest("code", "state")));
        ExchangeTelegramOidcCommand command = Assert.IsType<ExchangeTelegramOidcCommand>(sent);
        Assert.Equal("code", command.Code);
        Assert.Equal("state", command.State);
        Assert.Equal(new string('b', 43), command.BrowserBinding);
        Assert.Equal(intent.NextAction, Assert.IsType<TelegramAuthenticationIntentHttpResponse>(result.Value).NextAction);
    }

    private static AuthTelegramOnboardingController Create(ISender sender) {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = TelegramBrowserBindingHttpProcessor.CookieName + "=" + new string('b', 43);
        return new AuthTelegramOnboardingController(sender, new TelegramBrowserBindingHttpProcessor(TimeProvider.System)) {
            ControllerContext = new ControllerContext { HttpContext = context },
        };
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    public async Task Configuration_ReturnsProviderAvailability(bool login, bool registration, bool oidc) {
        var model = new TelegramConfigurationModel(login, registration, oidc);
        IRequest<Result<TelegramConfigurationModel>>? sent = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sent = request);
        var controller = new AuthTelegramOnboardingController(sender, new TelegramBrowserBindingHttpProcessor(TimeProvider.System)) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.Configuration());

        Assert.IsType<GetTelegramConfigurationQuery>(sent);
        TelegramConfigurationHttpResponse response = Assert.IsType<TelegramConfigurationHttpResponse>(result.Value);
        Assert.Multiple(
            () => Assert.Equal(login, response.LoginEnabled),
            () => Assert.Equal(registration, response.RegistrationEnabled),
            () => Assert.Equal(oidc, response.OidcEnabled));
    }
}
