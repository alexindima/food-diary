using FoodDiary.Application.Wearables.Common;
using FoodDiary.Application.Wearables.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application;

public static partial class DependencyInjection {
    private static void AddWearablesModule(this IServiceCollection services) {
        services.AddScoped<IWearableReadService, WearableReadService>();
    }
}
