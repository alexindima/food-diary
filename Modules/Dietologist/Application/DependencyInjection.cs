using FluentValidation;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Contracts.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Modules.Dietologist.Application.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Dietologist.Application;

public static class DependencyInjection {
    public static IServiceCollection AddDietologistApplication(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<IDietologistDashboardAccessService, DietologistDashboardAccessService>();
        services.AddScoped<IDietologistInvitationReadService, DietologistInvitationReadService>();
        services.AddScoped<IProfileDietologistReadService>(static provider =>
            (IProfileDietologistReadService)provider.GetRequiredService<IDietologistInvitationReadService>());
        services.AddScoped<IDietologistUserContextService, DietologistUserContextService>();
        services.AddScoped<IDietologistEmailSender, DietologistEmailSender>();

        return services;
    }
}
