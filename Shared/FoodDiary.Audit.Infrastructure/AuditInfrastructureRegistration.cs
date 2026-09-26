using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Audit.Infrastructure.Persistence;
using FoodDiary.Audit.Contracts.Audit.Common;
using FoodDiary.Audit.Contracts.Common.Abstractions.Audit;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Audit.Infrastructure;

public static class AuditInfrastructureRegistration {
    public static IServiceCollection AddAuditInfrastructure(this IServiceCollection services) {
        services.AddLogging();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IAuditLogger, StructuredAuditLogger>();
        services.AddScoped<AuditEntryService>();
        services.AddScoped<IAuditEntryReadService>(services => services.GetRequiredService<AuditEntryService>());
        services.AddScoped<IAuditEntryJournal>(services => services.GetRequiredService<AuditEntryService>());
        services.AddScoped<IAuditEntryWriter>(services => services.GetRequiredService<AuditEntryService>());
        return services;
    }
}
