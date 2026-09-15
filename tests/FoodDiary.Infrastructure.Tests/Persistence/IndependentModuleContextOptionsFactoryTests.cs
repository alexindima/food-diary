using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Modules.Ai.Infrastructure;
using FoodDiary.Modules.Ai.Infrastructure.Persistence;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class IndependentModuleContextOptionsFactoryTests {
    [Fact]
    public void ConfiguredOptionsPreserveProviderBehaviorWithoutJoiningScopedConnection() {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=independent_options_test;Username=test;Password=test",
                ["Database:EnableRetries"] = "true",
            }).Build()).AddOutboxProcessing(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=independent_options_test;Username=test;Password=test",
                ["Database:EnableRetries"] = "true",
            }).Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(Substitute.For<IDomainEventPublisher>());
        services.AddDbContext<SharedPersistenceDbContext>(builder => builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        services.AddAiPersistence();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IIndependentModuleContextOptionsFactory factory = scope.ServiceProvider.GetRequiredService<IIndependentModuleContextOptionsFactory>();
        DbContextOptions<AiDbContext> options = scope.ServiceProvider.GetRequiredService<DbContextOptions<AiDbContext>>();
        DbContextOptions<SharedPersistenceDbContext> configured = scope.ServiceProvider.GetRequiredService<DbContextOptions<SharedPersistenceDbContext>>();
        Assert.All(configured.Extensions, extension => Assert.Contains(options.Extensions, candidate => ReferenceEquals(candidate, extension)));
        using var first = new AiDbContext(options);
        using var second = new AiDbContext(factory.CreateOptions<AiDbContext>());
        SharedPersistenceDbContext shared = scope.ServiceProvider.GetRequiredService<SharedPersistenceDbContext>();
        Assert.Empty(shared.ModuleContexts);
        AiDbContext coordinated = scope.ServiceProvider.GetRequiredService<AiDbContext>();
        Assert.Multiple(
            () => Assert.Equal(QueryTrackingBehavior.NoTracking, first.ChangeTracker.QueryTrackingBehavior),
            () => Assert.True(first.Database.CreateExecutionStrategy().RetriesOnFailure),
            () => Assert.Equal(shared.Database.GetConnectionString(), first.Database.GetConnectionString()),
            () => Assert.NotSame(first.Database.GetDbConnection(), second.Database.GetDbConnection()),
            () => Assert.NotSame(shared.Database.GetDbConnection(), first.Database.GetDbConnection()),
            () => Assert.Same(shared.Database.GetDbConnection(), coordinated.Database.GetDbConnection()),
            () => Assert.Same(coordinated, Assert.Single(shared.ModuleContexts)));
    }
}
