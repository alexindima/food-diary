using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Infrastructure.Persistence.Dietologist;
using FoodDiary.Infrastructure.Persistence.Recommendations;
using FoodDiary.Infrastructure.Persistence.Audit;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddDietologistPersistence(this IServiceCollection services) {
        services.AddScoped<IDietologistInvitationRepository, DietologistInvitationRepository>();
        services.AddScoped<IDietologistInvitationReadRepository>(static provider => provider.GetRequiredService<IDietologistInvitationRepository>());
        services.AddScoped<IDietologistInvitationReadModelRepository>(static provider => provider.GetRequiredService<IDietologistInvitationRepository>());
        services.AddScoped<IDietologistInvitationWriteRepository>(static provider => provider.GetRequiredService<IDietologistInvitationRepository>());
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        services.AddScoped<IRecommendationReadRepository>(static provider => provider.GetRequiredService<IRecommendationRepository>());
        services.AddScoped<IRecommendationReadModelRepository>(static provider => provider.GetRequiredService<IRecommendationRepository>());
        services.AddScoped<IRecommendationWriteRepository>(static provider => provider.GetRequiredService<IRecommendationRepository>());
        services.AddScoped<IRecommendationCommentRepository, RecommendationCommentRepository>();
        services.AddScoped<IRecommendationCommentWriteRepository>(static provider => provider.GetRequiredService<IRecommendationCommentRepository>());
        services.AddScoped<IRecommendationCommentReadModelRepository>(static provider => provider.GetRequiredService<IRecommendationCommentRepository>());
        services.AddScoped<IClientTaskRepository, ClientTaskRepository>();
        services.AddScoped<IClientTaskWriteRepository>(static provider => provider.GetRequiredService<IClientTaskRepository>());
        services.AddScoped<IClientTaskReadModelRepository>(static provider => provider.GetRequiredService<IClientTaskRepository>());
        services.AddScoped<IRecommendationTemplateRepository, RecommendationTemplateRepository>();
        services.AddScoped<IRecommendationTemplateWriteRepository>(static provider => provider.GetRequiredService<IRecommendationTemplateRepository>());
        services.AddScoped<IRecommendationTemplateReadModelRepository>(static provider => provider.GetRequiredService<IRecommendationTemplateRepository>());
        services.AddScoped<IRecommendationBulkDispatchRepository, RecommendationBulkDispatchRepository>();
        services.AddScoped<IRecommendationBulkDispatchLookupRepository>(static provider => provider.GetRequiredService<IRecommendationBulkDispatchRepository>());
        services.AddScoped<IRecommendationBulkDispatchWriteRepository>(static provider => provider.GetRequiredService<IRecommendationBulkDispatchRepository>());
        services.AddScoped<IAttentionSignalMetricsReadService, AttentionSignalMetricsReadService>();
        services.AddScoped<AuditEntryService>();
        services.AddScoped<IAuditEntryReadService>(services => services.GetRequiredService<AuditEntryService>());
        services.AddScoped<IAuditEntryWriter>(services => services.GetRequiredService<AuditEntryService>());

    }
}
