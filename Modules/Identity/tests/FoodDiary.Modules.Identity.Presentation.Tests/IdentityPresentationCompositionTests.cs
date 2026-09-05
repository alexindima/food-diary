using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Security;
using FoodDiary.Presentation.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class IdentityPresentationCompositionTests {
    [Fact]
    public void AddIdentityPresentation_RegistersOwnedAdapters() {
        var services = new ServiceCollection();

        services.AddIdentityPresentation();

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IEmailVerificationNotifier) && descriptor.ImplementationType == typeof(EmailVerificationNotifier));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(RefreshTokenCookieService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(TelegramBotSecretAuthorizationFilter));
    }

    [Fact]
    public void MapIdentityPresentationHub_MapsRouteAndClosesExpiredAuthentication() {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Services.AddCors(options => options.AddPolicy("TestCors", policy => policy.AllowAnyOrigin()));
        builder.Services.AddSignalR();
        WebApplication app = builder.Build();

        app.MapIdentityPresentationHub("TestCors");

        RouteEndpoint[] endpoints = [.. ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>()];
        Assert.Contains(endpoints, endpoint => string.Equals(endpoint.RoutePattern.RawText, "/hubs/email-verification", StringComparison.Ordinal));
        MethodInfo method = typeof(IdentityPresentationApplicationBuilderExtensions).GetMethod("ConfigureAuthenticationLifetime", BindingFlags.NonPublic | BindingFlags.Static)!;
        var options = new HttpConnectionDispatcherOptions();
        method.Invoke(null, [options]);
        Assert.True(options.CloseOnAuthenticationExpiration);
    }
}
