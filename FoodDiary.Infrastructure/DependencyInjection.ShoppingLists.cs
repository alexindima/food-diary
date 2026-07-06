using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Infrastructure.Persistence.ShoppingLists;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddShoppingListPersistence(this IServiceCollection services) {
        services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
        services.AddScoped<IShoppingListReadRepository>(static provider => provider.GetRequiredService<IShoppingListRepository>());
        services.AddScoped<IShoppingListReadModelRepository>(static provider => provider.GetRequiredService<IShoppingListRepository>());
        services.AddScoped<IShoppingListWriteRepository>(static provider => provider.GetRequiredService<IShoppingListRepository>());

        return services;
    }
}
