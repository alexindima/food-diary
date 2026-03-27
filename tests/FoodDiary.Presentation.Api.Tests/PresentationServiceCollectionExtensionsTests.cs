using FoodDiary.Application.Authentication.Common;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Responses;
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
    public void AddPresentationApi_ConfiguresInvalidModelStateFactory_ToReturnApiErrorResponse() {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddOptions();
        services.AddPresentationApi();
        using var provider = services.BuildServiceProvider();

        var apiBehaviorOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiBehaviorOptions>>().Value;
        var httpContext = new DefaultHttpContext {
            RequestServices = provider,
            TraceIdentifier = "trace-123",
        };

        var actionContext = new ActionContext {
            HttpContext = httpContext,
        };
        actionContext.ModelState.AddModelError("email", "Email is required.");

        var result = apiBehaviorOptions.InvalidModelStateResponseFactory(actionContext);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiErrorHttpResponse>(badRequest.Value);
        Assert.Equal("Validation.Invalid", response.Error);
        Assert.Equal("One or more validation errors occurred.", response.Message);
        Assert.Equal("trace-123", response.TraceId);
        Assert.NotNull(response.Errors);
        Assert.True(response.Errors.TryGetValue("email", out var errors));
        Assert.Equal(["Email is required."], errors);
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
