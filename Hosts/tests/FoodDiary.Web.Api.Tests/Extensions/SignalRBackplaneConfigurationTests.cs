using System.Net;
using FoodDiary.Modules.Notifications.Presentation.Hubs;
using FoodDiary.Web.Api.Extensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class SignalRBackplaneConfigurationTests {
    [Fact]
    public void RedisConfiguration_UsesExistingConnectionAndStableApplicationChannels() {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:Redis"] = "localhost:6389,defaultDatabase=4,abortConnect=false",
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging().AddApiAuthentication(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        RedisOptions options = provider.GetRequiredService<IOptions<RedisOptions>>().Value;
        DnsEndPoint endpoint = Assert.IsType<DnsEndPoint>(Assert.Single(options.Configuration.EndPoints));

        Assert.Multiple(
            () => Assert.Equal("localhost", endpoint.Host),
            () => Assert.Equal(6389, endpoint.Port),
            () => Assert.Equal(4, options.Configuration.DefaultDatabase),
            () => Assert.False(options.Configuration.AbortOnConnectFail),
            () => Assert.Equal("fooddiary:signalr", options.Configuration.ChannelPrefix.ToString()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void DevelopmentWithoutRedis_RetainsInProcessSignalR(string? redis) {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:Redis"] = redis,
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging().AddApiAuthentication(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsType<DefaultHubLifetimeManager<NotificationHub>>(provider.GetRequiredService<HubLifetimeManager<NotificationHub>>());
    }
}
