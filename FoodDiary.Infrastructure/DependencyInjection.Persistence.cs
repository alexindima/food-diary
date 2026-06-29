using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Infrastructure.Events;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Interceptors;
using FoodDiary.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration) {
        services.AddSingleton<DatabaseCommandTelemetryInterceptor>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
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
                    sp.GetRequiredService<DomainEventDispatchInterceptor>());
        });

        return services;
    }
}
