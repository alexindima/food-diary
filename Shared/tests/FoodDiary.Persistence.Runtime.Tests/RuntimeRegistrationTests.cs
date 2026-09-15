using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Persistence.Runtime.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FoodDiary.Persistence.Runtime.Tests;

[ExcludeFromCodeCoverage]
public sealed class RuntimeRegistrationTests {
    [Fact]
    public void RuntimeResolvesAndBuildsSharedModelWithoutFullModelAssembly() {
        ServiceCollection services = CreateServices();
        Assert.DoesNotContain(services, descriptor =>
            descriptor.ServiceType.FullName?.Contains("FoodDiaryDbContext", StringComparison.Ordinal) == true);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        SharedPersistenceDbContext context = scope.ServiceProvider.GetRequiredService<SharedPersistenceDbContext>();
        Assert.IsType<SharedRuntimeDbContext>(context);
        Assert.Equal(["AuditEntry", "EmailOutboxMessage", "OutboxReplayAudit"],
            context.Model.GetEntityTypes().Select(entity => entity.ClrType.Name).Order(StringComparer.Ordinal), StringComparer.Ordinal);
        Assert.Empty(context.Database.GetMigrations());
    }

    [Fact]
    public void FactoryBorrowsScopedConnectionAndScopesStayIndependent() {
        using ServiceProvider provider = CreateServices().BuildServiceProvider();
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();
        SharedPersistenceDbContext root = first.ServiceProvider.GetRequiredService<SharedPersistenceDbContext>();
        IModuleContextFactory factory = first.ServiceProvider.GetRequiredService<IModuleContextFactory>();
        using ProbeContext participant = factory.CreateModuleContext<ProbeContext>(static options => new ProbeContext(options));
        Assert.Same(root.Database.GetDbConnection(), participant.Database.GetDbConnection());
        Assert.NotSame(root.Database.GetDbConnection(), second.ServiceProvider
            .GetRequiredService<SharedPersistenceDbContext>().Database.GetDbConnection());
    }

    private static ServiceCollection CreateServices() {
        var services = new ServiceCollection();
        services.AddPersistenceRuntime(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=runtime_model;Username=test",
            }).Build());
        services.AddSingleton(Substitute.For<IDomainEventPublisher>());
        return services;
    }

    [ExcludeFromCodeCoverage]
    private sealed class ProbeContext(DbContextOptions<ProbeContext> options) : DbContext(options);
}
