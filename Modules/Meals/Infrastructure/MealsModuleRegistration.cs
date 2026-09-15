using FoodDiary.Modules.Meals.Infrastructure.Persistence.Meals;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.Meals.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Products.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Meals.Application;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Meals.Infrastructure;

public static class MealsModuleRegistration {
    public static IServiceCollection AddMealsModule(this IServiceCollection services) =>
        services.AddMealsApplication().AddMealsPersistence();

    public static IServiceCollection AddMealsPersistence(this IServiceCollection services) {
        services.AddScoped(provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<MealsDbContext>(options => new MealsDbContext(options)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, MealsUserDataPurgeParticipant>());
        services.AddScoped<IMealDailyCalorieReadService, MealDailyCalorieReadService>();
        services.AddScoped<IMealNutritionStatisticsReadService>(static provider => new MealNutritionStatisticsReadService(
            provider.GetRequiredService<MealsDbContext>().Meals, CreateTransactionSynchronizer(provider)));
        services.AddScoped<IMealRepository>(static provider => new MealRepository(
            provider.GetRequiredService<MealsDbContext>().Meals,
            provider.GetRequiredService<IMealProductNutritionQuery>(),
            provider.GetRequiredService<IProductSnapshotReadService>(),
            provider.GetRequiredService<IMealSourceSnapshotQuery>(), CreateTransactionSynchronizer(provider)));
        services.AddScoped<IMealRecognitionTransactionRunner, EfMealRecognitionTransactionRunner>();
        services.AddScoped<IMealRecognitionReceiptRepository>(static provider => new MealRecognitionReceiptRepository(
            provider.GetRequiredService<MealsDbContext>(), CreateTransactionSynchronizer(provider)));
        services.AddScoped<IMealReadRepository>(static provider => provider.GetRequiredService<IMealRepository>());
        services.AddScoped<IMealProjectionReadRepository>(static provider => provider.GetRequiredService<IMealRepository>());
        services.AddScoped<IMealActivityReadRepository>(static provider => provider.GetRequiredService<IMealRepository>());
        services.AddScoped<IMealProductNutritionReadRepository>(static provider => provider.GetRequiredService<IMealRepository>());
        services.AddScoped<IMealWriteRepository>(static provider => provider.GetRequiredService<IMealRepository>());

        return services;
    }
    private static Func<CancellationToken, Task> CreateTransactionSynchronizer(IServiceProvider provider) {
        IModuleTransactionCoordinator coordinator = provider.GetRequiredService<IModuleTransactionCoordinator>();
        MealsDbContext owned = provider.GetRequiredService<MealsDbContext>();
        return async cancellationToken => {
            if (owned.Database.IsRelational()) {
                await owned.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken).ConfigureAwait(false);
            }
        };
    }
}
