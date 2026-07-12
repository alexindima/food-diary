using System.Reflection;
using FluentValidation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Common.Behaviors;
using FoodDiary.Application.Common.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application;

public static partial class DependencyInjection {
    public static IServiceCollection AddApplication(this IServiceCollection services) {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddFoodDiaryMediator(cfg => {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(CommandTransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddScoped<IPostCommitActionQueue, PostCommitActionQueue>();
        services.AddSingleton(TimeProvider.System);

        services.AddAdministrationModules();
        services.AddIdentityModules();
        services.AddFoodModules();
        services.AddTrackingModules();
        services.AddNotificationModule();
        services.AddBillingModule();

        return services;
    }
}
