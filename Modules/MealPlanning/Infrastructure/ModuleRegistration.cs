using FoodDiary.Persistence.Abstractions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Modules.MealPlanning.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.MealPlanning;
using FoodDiary.Infrastructure.Persistence.MealPlans;
using FoodDiary.Infrastructure.Persistence.ShoppingLists;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.MealPlanning.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddMealPlanningModule(this IServiceCollection services) {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, MealPlanningUserDataPurgeParticipant>());
        services.AddMealPlanningApplication();
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<MealPlanningDbContext>(static options => new MealPlanningDbContext(options)));
        services.AddScoped<IMealPlanRepository>(static provider => new MealPlanRepository(
            provider.GetRequiredService<MealPlanningDbContext>().MealPlans, provider.GetRequiredService<IMealPlanCompositionReader>()));
        services.AddScoped<IMealPlanReadRepository>(static provider => provider.GetRequiredService<IMealPlanRepository>());
        services.AddScoped<IMealPlanReadModelRepository>(static provider => provider.GetRequiredService<IMealPlanRepository>());
        services.AddScoped<IMealPlanWriteRepository>(static provider => provider.GetRequiredService<IMealPlanRepository>());
        services.AddScoped<IShoppingListRepository>(static provider => new ShoppingListRepository(
            provider.GetRequiredService<MealPlanningDbContext>().ShoppingLists));
        services.AddScoped<IShoppingListReadRepository>(static provider => provider.GetRequiredService<IShoppingListRepository>());
        services.AddScoped<IShoppingListReadModelRepository>(static provider => provider.GetRequiredService<IShoppingListRepository>());
        services.AddScoped<IShoppingListWriteRepository>(static provider => provider.GetRequiredService<IShoppingListRepository>());
        return services;
    }
}
