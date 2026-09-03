using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Outbox;
using FoodDiary.Modules.Gamification.Infrastructure;
using FoodDiary.Modules.Images.Infrastructure;
using FoodDiary.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class OutboxReplayRegistrationTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Streams_AreScopedAndOwnedByTheirModules_RegardlessOfRegistrationOrder(bool modulesFirst) {
        var services = new ServiceCollection();
        if (modulesFirst) {
            AddModules(services);
        }
        services.AddInfrastructure(new ConfigurationBuilder().Build());
        if (!modulesFirst) {
            AddModules(services);
        }
        services.AddScoped(_ => new FoodDiaryDbContext(new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options));
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();
        IOutboxReplayStream[] streams = [.. first.ServiceProvider.GetServices<IOutboxReplayStream>().OrderBy(stream => stream.Order)];
        Assert.Equal(new[] { "email", "image_object_deletion", "notification_web_push", "achievement_evaluation" }, streams.Select(stream => stream.Name), StringComparer.Ordinal);
        Assert.Equal(new[] { "FoodDiary.Infrastructure", "FoodDiary.Modules.Images.Infrastructure", "FoodDiary.Modules.Notifications.Infrastructure", "FoodDiary.Modules.Gamification.Infrastructure" }, streams.Select(stream => stream.GetType().Assembly.GetName().Name), StringComparer.Ordinal);
        foreach (IOutboxReplayStream stream in streams) {
            Assert.Same(stream, first.ServiceProvider.GetServices<IOutboxReplayStream>().Single(item => string.Equals(item.Name, stream.Name, StringComparison.Ordinal)));
            Assert.NotSame(stream, second.ServiceProvider.GetServices<IOutboxReplayStream>().Single(item => string.Equals(item.Name, stream.Name, StringComparison.Ordinal)));
        }
        Assert.All(services.Where(descriptor => descriptor.ServiceType == typeof(IOutboxReplayStream)), descriptor => Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime));
        Assert.NotNull(streams[0].ReplayRejectionReason);
        Assert.All(streams.Skip(1), stream => Assert.Null(stream.ReplayRejectionReason));
    }

    [Fact]
    public void DuplicateNames_AreRejectedRatherThanSilentlySelectingAnAdapter() {
        using var context = new FoodDiaryDbContext(new DbContextOptionsBuilder<FoodDiaryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        IOutboxReplayStream stream = Substitute.For<IOutboxReplayStream>();
        stream.Name.Returns("duplicate");
        Assert.Throws<ArgumentException>(() => new OutboxDeadLetterReplayService(context, TimeProvider.System, [stream, stream]));
    }

    [Fact]
    public void CentralInfrastructure_RegistersOnlyItsEmailStream() {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build());
        ServiceDescriptor descriptor = Assert.Single(services, item => item.ServiceType == typeof(IOutboxReplayStream));
        Assert.Equal("FoodDiary.Infrastructure.Persistence.Email.EmailOutboxReplayStream", descriptor.ImplementationType?.FullName);
    }

    private static void AddModules(IServiceCollection services) {
        services.AddGamificationModule();
        services.AddNotificationsPersistence();
        services.AddImagesInfrastructure();
        services.AddGamificationModule();
        services.AddNotificationsPersistence();
        services.AddImagesInfrastructure();
    }
}
