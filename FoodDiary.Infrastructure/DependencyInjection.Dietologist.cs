using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Infrastructure.Persistence.Dietologist;
using FoodDiary.Infrastructure.Persistence.Recommendations;
using FoodDiary.Infrastructure.Persistence.Audit;
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
        services.AddScoped<IRecommendationCommentRepository, RecommendationCommentRepository>();
        services.AddScoped<IClientTaskRepository, ClientTaskRepository>();
        services.AddScoped<IRecommendationTemplateRepository, RecommendationTemplateRepository>();
        services.AddScoped<IRecommendationBulkDispatchRepository, RecommendationBulkDispatchRepository>();
        services.AddScoped<AuditEntryService>();
        services.AddScoped<IAuditEntryReadService>(services => services.GetRequiredService<AuditEntryService>());
        services.AddScoped<IAuditEntryWriter>(services => services.GetRequiredService<AuditEntryService>());

        return services;
    }
}
