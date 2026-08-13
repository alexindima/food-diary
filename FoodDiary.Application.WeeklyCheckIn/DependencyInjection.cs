using FluentValidation;
using FoodDiary.Application.WeeklyCheckIn.Common;
using FoodDiary.Application.WeeklyCheckIn.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.WeeklyCheckIn;

public static class DependencyInjection {
    public static IServiceCollection AddWeeklyCheckInModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IWeeklyCheckInUserProfileService, WeeklyCheckInUserProfileService>();
        services.AddScoped<IWeeklyCheckInReadService, WeeklyCheckInReadService>();
        return services;
    }
}
