using FoodDiary.Outbox.Infrastructure.Persistence;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;
using FoodDiary.Infrastructure.Events;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Infrastructure.Persistence.Interceptors;
using FoodDiary.Infrastructure.Persistence.Outbox;
using FoodDiary.Infrastructure.Persistence.Shared;
using FoodDiary.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration) {
        services.AddSingleton<DatabaseCommandTelemetryInterceptor>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IModuleTransactionCoordinator, EfModuleTransactionCoordinator>();
        services.AddScoped<IModuleSessionCoordinator, EfModuleSessionCoordinator>();
        services.AddScoped<IModuleSessionLock, EfModuleSessionLock>();
        services.AddScoped<IModuleScopeGuard, EfModuleScopeGuard>();
        services.AddScoped<IIndependentModuleContextOptionsFactory, EfIndependentModuleContextOptionsFactory>();
        services.AddScoped<IModuleContextFactory>(static provider => provider.GetRequiredService<FoodDiaryDbContext>());
        services.AddScoped<IOutboxDeadLetterReplayService, OutboxDeadLetterReplayService>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IOutboxReplayStream, EmailOutboxReplayStream>());
        services.AddScoped<IDomainEventPublisher, MediatorDomainEventPublisher>();
        services.AddScoped<DomainEventDispatchInterceptor>();
        services.AddDbContext<FoodDiaryDbContext>((sp, options) => {
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

    }
}
