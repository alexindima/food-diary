using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Infrastructure.Persistence.Audit;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddAuditPersistence(this IServiceCollection services) {
        services.AddScoped<AuditEntryService>();
        services.AddScoped<IAuditEntryReadService>(services => services.GetRequiredService<AuditEntryService>());
        services.AddScoped<IAuditEntryJournal>(services => services.GetRequiredService<AuditEntryService>());
        services.AddScoped<IAuditEntryWriter>(services => services.GetRequiredService<AuditEntryService>());
    }
}
