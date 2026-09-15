using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Outbox.Infrastructure.Options;
using FoodDiary.Persistence.Runtime.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Persistence.Runtime.Tests;

[ExcludeFromCodeCoverage]
public sealed class OutboxOptionsRegistrationTests {
    [Fact]
    public void PersistenceOnlyDoesNotValidateUnconfiguredOutbox() {
        var services = new ServiceCollection();
        services.AddPersistenceRuntime(CreateConfiguration("00:00:01"));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IValidateOptions<OutboxProcessingOptions>));
        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IStartupValidator>().Validate();
        Assert.NotNull(provider.GetRequiredService<IOptions<DatabaseOptions>>().Value);
    }

    [Fact]
    public void ExplicitOutboxRegistrationRejectsUnsafeLeaseAtStartup() {
        var services = new ServiceCollection();
        services.AddOutboxProcessing(CreateConfiguration("00:00:01"));
        using ServiceProvider provider = services.BuildServiceProvider();
        OptionsValidationException error = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());
        Assert.Contains("LeaseDuration", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ExplicitOutboxRegistrationBindsConfiguredDurations() {
        var services = new ServiceCollection();
        services.AddOutboxProcessing(CreateConfiguration("00:10:00"));
        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IStartupValidator>().Validate();
        OutboxProcessingOptions options = provider.GetRequiredService<IOptions<OutboxProcessingOptions>>().Value;
        Assert.Equal(TimeSpan.FromMinutes(10), options.LeaseDuration);
        Assert.Equal(TimeSpan.FromMinutes(4), options.DispatchTimeout);
        Assert.Equal(TimeSpan.FromSeconds(15), options.FinalizationTimeout);
    }

    private static IConfiguration CreateConfiguration(string lease) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["OutboxProcessing:LeaseDuration"] = lease,
        }).Build();
}
