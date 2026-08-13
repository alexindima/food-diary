using FluentValidation;
using FoodDiary.Application.Tdee.Common;
using FoodDiary.Application.Tdee.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Tdee;

public static class DependencyInjection {
    public static IServiceCollection AddTdeeModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<ITdeeUserProfileService, TdeeUserProfileService>();
        return services;
    }
}
