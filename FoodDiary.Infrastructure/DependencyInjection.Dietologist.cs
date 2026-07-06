using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Infrastructure.Persistence.Dietologist;
using FoodDiary.Infrastructure.Persistence.Recommendations;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddDietologistPersistence(this IServiceCollection services) {
        services.AddScoped<IDietologistInvitationRepository, DietologistInvitationRepository>();
        services.AddScoped<IDietologistInvitationReadRepository>(static provider => provider.GetRequiredService<IDietologistInvitationRepository>());
        services.AddScoped<IDietologistInvitationReadModelRepository>(static provider => provider.GetRequiredService<IDietologistInvitationRepository>());
        services.AddScoped<IDietologistInvitationWriteRepository>(static provider => provider.GetRequiredService<IDietologistInvitationRepository>());
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        services.AddScoped<IRecommendationReadRepository>(static provider => provider.GetRequiredService<IRecommendationRepository>());
        services.AddScoped<IRecommendationReadModelRepository>(static provider => provider.GetRequiredService<IRecommendationRepository>());
        services.AddScoped<IRecommendationWriteRepository>(static provider => provider.GetRequiredService<IRecommendationRepository>());

        return services;
    }
}
