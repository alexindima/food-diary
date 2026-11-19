using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Infrastructure.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Billing.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class BillingTransportRegistrationTests {
    [Fact]
    public void RegisteredTelegramMessageClientUsesTheBoundedTransportTimeout() {
        var services = new ServiceCollection();
        services.AddBillingIntegrations(new ConfigurationBuilder().Build());
        using ServiceProvider provider = services.BuildServiceProvider();
        using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IBillingTelegramMessageSender));

        Assert.Equal(TimeSpan.FromSeconds(8), client.Timeout);
    }
}
