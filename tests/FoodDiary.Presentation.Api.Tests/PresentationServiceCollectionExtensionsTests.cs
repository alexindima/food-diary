using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class PresentationServiceCollectionExtensionsTests {
    [Fact]
    public void AddPresentationApi_RegistersPresentationServices() {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddPresentationApi();
        using var provider = services.BuildServiceProvider();

        var emailVerificationNotifier = provider.GetRequiredService<IEmailVerificationNotifier>();
        var userIdProvider = provider.GetRequiredService<IUserIdProvider>();

        Assert.IsType<EmailVerificationNotifier>(emailVerificationNotifier);
        Assert.IsType<UserIdProvider>(userIdProvider);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IActionDescriptorCollectionProvider));
    }

    [Fact]
    public void AddPresentationApi_ConfiguresInvalidModelStateFactory_ToReturnUnauthorizedForCurrentUserBindingFailures() {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddOptions();
        services.AddPresentationApi();
        using var provider = services.BuildServiceProvider();

        var apiBehaviorOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiBehaviorOptions>>().Value;
        var httpContext = new DefaultHttpContext {
            RequestServices = provider,
        };
        httpContext.Items[CurrentUserIdModelBinder.UnauthorizedItemKey] = true;

        var actionContext = new ActionContext {
            HttpContext = httpContext,
        };

        var result = apiBehaviorOptions.InvalidModelStateResponseFactory(actionContext);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void MapPresentationApi_MapsEmailVerificationHub() {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddCors(options => options.AddPolicy("TestCors", policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
        builder.Services.AddAuthorization();
        builder.Services.AddPresentationApi();

        var app = builder.Build();

        app.MapPresentationApi("TestCors");

        var endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();

        Assert.Contains(endpoints, endpoint => endpoint.RoutePattern.RawText == "/hubs/email-verification");
        Assert.Contains(endpoints, endpoint => endpoint.RoutePattern.RawText == "/hubs/email-verification/negotiate");
    }
}
