using FluentValidation;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Dietologist;

public static class DependencyInjection {
    public static IServiceCollection AddDietologistModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<IDietologistClientReadService, DietologistClientReadService>();
        services.AddScoped<IDietologistDashboardAccessService, DietologistDashboardAccessService>();
        services.AddScoped<IDietologistInvitationReadService, DietologistInvitationReadService>();
        services.AddScoped<IProfileDietologistReadService>(static provider =>
            (IProfileDietologistReadService)provider.GetRequiredService<IDietologistInvitationReadService>());
        services.AddScoped<IDietologistRecommendationReadService, DietologistRecommendationReadService>();
        services.AddScoped<IRecommendationDiscussionReadService, RecommendationDiscussionReadService>();
        services.AddScoped<IRecommendationTemplateReadService, RecommendationTemplateReadService>();
        services.AddScoped<IDietologistUserLookupService, DietologistUserLookupService>();
        services.AddScoped<IDietologistUserContextService, DietologistUserContextService>();
        services.AddScoped<IDietologistEmailSender, DietologistEmailSender>();

        return services;
    }
}
