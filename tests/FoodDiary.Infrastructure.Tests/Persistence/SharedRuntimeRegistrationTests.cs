using FoodDiary.Persistence.Runtime;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Audit;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Infrastructure.Persistence.Outbox;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class SharedRuntimeRegistrationTests {
    [Fact]
    public void CompositionFacadeUsesSameContextAndExposesOnlyQueryableProperties() {
        using ServiceProvider provider = CreateProvider();
        using IServiceScope scope = provider.CreateScope();
        ICompositionReadContext reader = scope.ServiceProvider.GetRequiredService<ICompositionReadContext>();
        FoodDiaryDbContext full = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        Assert.Same(full, reader);
        Assert.All(typeof(ICompositionReadContext).GetProperties(), property => {
            Assert.False(property.CanWrite);
            Assert.Equal(typeof(IQueryable<>), property.PropertyType.GetGenericTypeDefinition());
            var query = (IQueryable)property.GetValue(reader)!;
            Assert.Contains("AsNoTracking", query.Expression.ToString(), StringComparison.Ordinal);
        });
        Assert.All(typeof(ICompositionReadContext).GetMethods(), method => Assert.True(method.IsSpecialName));
    }

    [Fact]
    public void RuntimeModelContainsOnlySharedRecordsAndDoesNotResolveFullModel() {
        using ServiceProvider provider = CreateProvider();
        using IServiceScope scope = provider.CreateScope();
        SharedPersistenceDbContext runtime = scope.ServiceProvider.GetRequiredService<SharedPersistenceDbContext>();
        Assert.IsType<SharedRuntimeDbContext>(runtime);
        Assert.Equal(new[] { typeof(AuditEntry), typeof(EmailOutboxMessage), typeof(OutboxReplayAudit) },
            runtime.Model.GetEntityTypes().Select(entity => entity.ClrType).OrderBy(type => type.Name, StringComparer.Ordinal));
        Assert.Empty(runtime.ModuleContexts);
        Assert.Same(runtime.Session, scope.ServiceProvider.GetRequiredService<IModuleContextFactory>());
    }

    [Fact]
    public void FullModelIsSeparateAndJoinsTheScopedConnectionWhenRequested() {
        using ServiceProvider provider = CreateProvider();
        using IServiceScope scope = provider.CreateScope();
        SharedPersistenceDbContext runtime = scope.ServiceProvider.GetRequiredService<SharedPersistenceDbContext>();
        FoodDiaryDbContext full = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        Assert.NotSame(runtime, full);
        Assert.Same(runtime.Session, full.Session);
        Assert.Same(runtime.Database.GetDbConnection(), full.Database.GetDbConnection());
        Assert.Contains(full, runtime.ModuleContexts);
        Assert.True(full.Model.GetEntityTypes().Count() > runtime.Model.GetEntityTypes().Count());
        Assert.NotEmpty(full.Database.GetMigrations());
        Assert.Empty(runtime.Database.GetMigrations());
        Assert.False(full.Database.HasPendingModelChanges());
    }

    private static ServiceProvider CreateProvider() {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=runtime_model;Username=test",
            }).Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(Substitute.For<IDomainEventPublisher>());
        return services.BuildServiceProvider();
    }
}
