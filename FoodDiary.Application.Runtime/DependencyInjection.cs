using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Runtime.Common.Behaviors;
using FoodDiary.Application.Runtime.Common.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Runtime;

public static class DependencyInjection {
    public static IServiceCollection AddApplicationRuntime(this IServiceCollection services) {
        services.AddFoodDiaryMediator(cfg => {
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(CommandTransactionBehavior<,>));
        });

        services.AddScoped<IPostCommitActionQueue, PostCommitActionQueue>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
