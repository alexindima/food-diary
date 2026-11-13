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
    public async Task PersistenceOnlyDoesNotValidateUnconfiguredOutboxAsync() {
        var services = new ServiceCollection();
        services.AddPersistenceRuntime(CreateConfiguration("00:00:01"));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IValidateOptions<OutboxProcessingOptions>));
        await using ServiceProvider provider = services.BuildServiceProvider();
        await provider.GetRequiredService<IAsyncStartupValidator>().ValidateAsync(CancellationToken.None);
        Assert.NotNull(provider.GetRequiredService<IOptions<DatabaseOptions>>().Value);
    }

    [Fact]
    public async Task ExplicitOutboxRegistrationRejectsUnsafeLeaseAtStartupAsync() {
        var services = new ServiceCollection();
        services.AddOutboxProcessing(CreateConfiguration("00:00:01"));
        await using ServiceProvider provider = services.BuildServiceProvider();
        OptionsValidationException error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
            await provider.GetRequiredService<IAsyncStartupValidator>().ValidateAsync(CancellationToken.None));
        Assert.Contains("LeaseDuration", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExplicitOutboxRegistrationBindsConfiguredDurationsAsync() {
        var services = new ServiceCollection();
        services.AddOutboxProcessing(CreateConfiguration("00:10:00"));
        await using ServiceProvider provider = services.BuildServiceProvider();
        await provider.GetRequiredService<IAsyncStartupValidator>().ValidateAsync(CancellationToken.None);
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
