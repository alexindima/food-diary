using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Security.Claims;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Web.Api.Extensions;
using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class HealthConsentMiddlewareTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HealthMutation_RequiresAnActiveConsent(bool granted) {
        await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Response.Body = new MemoryStream();
        var id = Guid.NewGuid();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], "test"));
        context.Request.Method = "PATCH";
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new ControllerActionDescriptor {
            ControllerName = "Users",
            ActionName = "UpdateDesiredWeight",
            ControllerTypeInfo = typeof(HealthConsentMiddlewareTests).GetTypeInfo(),
        }), "health-test"));
        IUserPrivacyStateReader privacy = Substitute.For<IUserPrivacyStateReader>();
        privacy.GetAsync(new UserId(id), Arg.Any<CancellationToken>()).Returns(new UserPrivacyStateModel("2026-11-02", granted ? DateTime.UtcNow : null, AiAcceptedAtUtc: null, MarketingAcceptedAtUtc: null));
        bool continued = false;
        var middleware = new HealthConsentMiddleware(_ => { continued = true; return Task.CompletedTask; }, MsOptions.Create(new PrivacyOptions()));

        await middleware.InvokeAsync(context, privacy);

        Assert.Equal(granted, continued);
        Assert.Equal(granted ? 200 : 403, context.Response.StatusCode);
    }

    [Fact]
    public async Task Erasure_RemainsAvailableWithoutHealthConsent() {
        await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = "DELETE";
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "test"));
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new ControllerActionDescriptor {
            ControllerName = "Users",
            ActionName = "UpdateDesiredWeight",
            ControllerTypeInfo = typeof(HealthConsentMiddlewareTests).GetTypeInfo(),
        }), "health-delete-test"));
        bool continued = false;
        IUserPrivacyStateReader privacy = Substitute.For<IUserPrivacyStateReader>();
        var middleware = new HealthConsentMiddleware(_ => { continued = true; return Task.CompletedTask; }, MsOptions.Create(new PrivacyOptions()));
        await middleware.InvokeAsync(context, privacy);
        Assert.True(continued);
        await privacy.DidNotReceiveWithAnyArgs().GetAsync(default, default);
    }
}
