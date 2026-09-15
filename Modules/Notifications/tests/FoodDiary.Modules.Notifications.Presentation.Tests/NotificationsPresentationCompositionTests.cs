using FoodDiary.Modules.Notifications.Presentation.Services;
using FoodDiary.Modules.Notifications.Presentation.Extensions;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Presentation.Api.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace FoodDiary.Modules.Notifications.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class NotificationsPresentationCompositionTests {
    [Fact]
    public void AddNotificationsPresentation_RegistersOwnedAdapter() {
        var services = new ServiceCollection();

        services.AddNotificationsPresentation();

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(INotificationPusher) && descriptor.ImplementationType == typeof(NotificationPusher));
    }

    [Fact]
    public void MapNotificationsPresentationHub_MapsRouteAndClosesExpiredAuthentication() {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Services.AddCors(options => options.AddPolicy("TestCors", policy => policy.AllowAnyOrigin()));
        builder.Services.AddSignalR();
        WebApplication app = builder.Build();

        app.MapNotificationsPresentationHub("TestCors");

        RouteEndpoint[] endpoints = [.. ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>()];
        Assert.Contains(endpoints, endpoint => string.Equals(endpoint.RoutePattern.RawText, "/hubs/notifications", StringComparison.Ordinal));
        MethodInfo method = typeof(NotificationsPresentationApplicationBuilderExtensions).GetMethod("ConfigureAuthenticationLifetime", BindingFlags.NonPublic | BindingFlags.Static)!;
        var options = new HttpConnectionDispatcherOptions();
        method.Invoke(null, [options]);
        Assert.True(options.CloseOnAuthenticationExpiration);
    }
}
