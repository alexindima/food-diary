using FoodDiary.Application.Abstractions.DailyAdvices.Common;
using FoodDiary.Application.DailyAdvices;
using FoodDiary.Modules.DailyAdvices.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.DailyAdvices.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddDailyAdvicesModule(this IServiceCollection services) {
        services.AddDailyAdvicesApplication();
        services.AddScoped<IDailyAdviceReadModelRepository, DailyAdviceRepository>();
        return services;
    }
}
