using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Interceptors;
using FoodDiary.Infrastructure.Services;
using FoodDiary.Modules.Dietologist.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class CollaborationAuditRegistrationTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddDietologistModule_RegistersOneScopedAuditAfterDomainEvents(bool moduleFirst) {
        var services = new ServiceCollection();
        if (moduleFirst) {
            services.AddDietologistModule().AddDietologistModule();
        }

        AddCentralPersistence(services);
        if (!moduleFirst) {
            services.AddDietologistModule().AddDietologistModule();
        }

        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();
        CollaborationAuditInterceptor audit = Assert.IsType<CollaborationAuditInterceptor>(
            Assert.Single(first.ServiceProvider.GetServices<ISaveChangesInterceptor>()));
        Assert.Same(typeof(ModuleRegistration).Assembly, audit.GetType().Assembly);
        Assert.Same(audit, Assert.Single(first.ServiceProvider.GetServices<ISaveChangesInterceptor>()));
        Assert.NotSame(audit, Assert.Single(second.ServiceProvider.GetServices<ISaveChangesInterceptor>()));

        DbContextOptions<FoodDiaryDbContext> options = first.ServiceProvider.GetRequiredService<DbContextOptions<FoodDiaryDbContext>>();
        CoreOptionsExtension core = Assert.IsType<CoreOptionsExtension>(options.FindExtension<CoreOptionsExtension>());
        Assert.Collection(core.Interceptors ?? [],
            interceptor => Assert.IsType<DatabaseCommandTelemetryInterceptor>(interceptor),
            interceptor => Assert.IsType<DomainEventDispatchInterceptor>(interceptor),
            interceptor => Assert.Same(audit, interceptor));
        Assert.NotNull(first.ServiceProvider.GetRequiredService<FoodDiaryDbContext>());
    }

    [Fact]
    public void AddInfrastructure_WithoutDietologist_DoesNotOwnCollaborationRules() {
        var services = new ServiceCollection();
        AddCentralPersistence(services);

        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        using IServiceScope scope = provider.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetServices<ISaveChangesInterceptor>());
        DbContextOptions<FoodDiaryDbContext> options = scope.ServiceProvider.GetRequiredService<DbContextOptions<FoodDiaryDbContext>>();
        CoreOptionsExtension core = Assert.IsType<CoreOptionsExtension>(options.FindExtension<CoreOptionsExtension>());
        Assert.Collection(core.Interceptors ?? [],
            interceptor => Assert.IsType<DatabaseCommandTelemetryInterceptor>(interceptor),
            interceptor => Assert.IsType<DomainEventDispatchInterceptor>(interceptor));
    }

    private static void AddCentralPersistence(IServiceCollection services) {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=audit_composition;Username=test;Password=test",
            }).Build();
        services.AddInfrastructure(configuration);
        services.AddScoped(_ => Substitute.For<IDomainEventPublisher>());
    }
}
