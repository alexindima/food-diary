using FoodDiary.Persistence.Runtime.Persistence.Audit;
using FoodDiary.Application.Abstractions.Audit.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Persistence.Runtime;

public static partial class PersistenceRuntimeRegistration {
    private static void AddAuditPersistence(this IServiceCollection services) {
        services.AddScoped<AuditEntryService>();
        services.AddScoped<IAuditEntryReadService>(services => services.GetRequiredService<AuditEntryService>());
        services.AddScoped<IAuditEntryJournal>(services => services.GetRequiredService<AuditEntryService>());
        services.AddScoped<IAuditEntryWriter>(services => services.GetRequiredService<AuditEntryService>());
    }
}
