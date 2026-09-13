using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Dietologist.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Dietologist;
using FoodDiary.Infrastructure.Persistence.Dietologist;
using FoodDiary.Infrastructure.Persistence.Interceptors;
using FoodDiary.Infrastructure.Persistence.Recommendations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Modules.Dietologist.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddDietologistModule(this IServiceCollection services) {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, DietologistUserDataPurgeParticipant>());
        services.AddDietologistApplication();
        services.AddScoped(static provider => provider.GetRequiredService<FoodDiaryDbContext>()
            .CreateModuleContext<DietologistDbContext>(static options => new DietologistDbContext(options)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISaveChangesInterceptor, CollaborationAuditInterceptor>());
        services.AddScoped<IDietologistInvitationRepository, DietologistInvitationRepository>();
        services.AddScoped<IDietologistInvitationReadRepository>(static provider => provider.GetRequiredService<IDietologistInvitationRepository>());
        services.AddScoped<IDietologistInvitationWriteRepository>(static provider => provider.GetRequiredService<IDietologistInvitationRepository>());
        services.AddScoped<IRecommendationRepository>(static provider => new RecommendationRepository(provider.GetRequiredService<DietologistDbContext>().Recommendations, provider.GetRequiredService<IRecommendationReadModelRepository>()));
        services.AddScoped<IRecommendationReadRepository>(static provider => provider.GetRequiredService<IRecommendationRepository>());
        services.AddScoped<IRecommendationWriteRepository>(static provider => provider.GetRequiredService<IRecommendationRepository>());
        services.AddScoped<IRecommendationCommentRepository>(static provider => new RecommendationCommentRepository(provider.GetRequiredService<DietologistDbContext>().RecommendationComments, provider.GetRequiredService<IRecommendationCommentReadModelRepository>()));
        services.AddScoped<IRecommendationCommentWriteRepository>(static provider => provider.GetRequiredService<IRecommendationCommentRepository>());
        services.AddScoped<IClientTaskRepository>(static provider => new ClientTaskRepository(provider.GetRequiredService<DietologistDbContext>().ClientTasks));
        services.AddScoped<IClientTaskWriteRepository>(static provider => provider.GetRequiredService<IClientTaskRepository>());
        services.AddScoped<IClientTaskReadModelRepository>(static provider => provider.GetRequiredService<IClientTaskRepository>());
        services.AddScoped<IRecommendationTemplateRepository>(static provider => new RecommendationTemplateRepository(provider.GetRequiredService<DietologistDbContext>().RecommendationTemplates));
        services.AddScoped<IRecommendationTemplateWriteRepository>(static provider => provider.GetRequiredService<IRecommendationTemplateRepository>());
        services.AddScoped<IRecommendationTemplateReadModelRepository>(static provider => provider.GetRequiredService<IRecommendationTemplateRepository>());
        services.AddScoped<IRecommendationBulkDispatchRepository>(static provider => new RecommendationBulkDispatchRepository(provider.GetRequiredService<DietologistDbContext>().RecommendationBulkDispatches));
        services.AddScoped<IRecommendationBulkDispatchLookupRepository>(static provider => provider.GetRequiredService<IRecommendationBulkDispatchRepository>());
        services.AddScoped<IRecommendationBulkDispatchWriteRepository>(static provider => provider.GetRequiredService<IRecommendationBulkDispatchRepository>());
        return services;
    }
}
