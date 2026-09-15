using FoodDiary.Persistence.Runtime.Events;
using FoodDiary.Persistence.Runtime.Persistence.Interceptors;
using FoodDiary.Persistence.Runtime.Persistence.Shared;
using FoodDiary.Persistence.Runtime.Services;
using FoodDiary.Persistence.Runtime.Options;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace FoodDiary.Persistence.Runtime;

public static partial class PersistenceRuntimeRegistration {
    public static IServiceCollection AddPersistenceRuntime(this IServiceCollection services, IConfiguration configuration) {
        services.TryAddSingleton(TimeProvider.System);
        services.AddLogging();
        services.AddPersistenceOptions(configuration);
        services.AddSingleton<DatabaseCommandTelemetryInterceptor>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IModuleTransactionCoordinator, EfModuleTransactionCoordinator>();
        services.AddScoped<IAtomicCommandExecutor, EfAtomicCommandExecutor>();
        services.AddScoped<IModuleSessionCoordinator, EfModuleSessionCoordinator>();
        services.AddScoped<IModuleSessionLock, EfModuleSessionLock>();
        services.AddScoped<IModuleScopeGuard, EfModuleScopeGuard>();
        services.AddScoped<IIndependentModuleContextOptionsFactory>(static provider =>
            new EfIndependentModuleContextOptionsFactory(provider.GetRequiredService<DbContextOptions<SharedPersistenceDbContext>>()));
        services.AddScoped<PersistenceSession>(static provider => provider.GetRequiredService<SharedPersistenceDbContext>().Session);
        services.AddScoped<IModuleContextFactory>(static provider => provider.GetRequiredService<PersistenceSession>());
        services.AddScoped<IDomainEventPublisher, MediatorDomainEventPublisher>();
        services.AddScoped<DomainEventDispatchInterceptor>();
        services.AddDbContext<SharedPersistenceDbContext>((sp, options) => {
            DatabaseOptions databaseOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options
                .UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => {
                        if (databaseOptions.EnableRetries) {
                            npgsqlOptions.EnableRetryOnFailure(
                                databaseOptions.MaxRetryCount,
                                TimeSpan.FromSeconds(databaseOptions.MaxRetryDelaySeconds),
                                errorCodesToAdd: null);
                        }
                    })
                .AddInterceptors(
                    sp.GetRequiredService<DatabaseCommandTelemetryInterceptor>(),
                    sp.GetRequiredService<DomainEventDispatchInterceptor>())
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
        });

        services.Replace(ServiceDescriptor.Scoped<SharedPersistenceDbContext, SharedRuntimeDbContext>());
        return services;
    }
}
