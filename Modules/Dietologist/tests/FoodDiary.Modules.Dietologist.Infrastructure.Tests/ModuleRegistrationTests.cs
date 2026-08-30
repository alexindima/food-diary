using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Infrastructure.Persistence.Dietologist;
using FoodDiary.Infrastructure.Persistence.Recommendations;
using FoodDiary.Modules.Dietologist.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class ModuleRegistrationTests {
    [Fact]
    public void AddDietologistModule_RegistersEveryOwnedPersistencePort() {
        var services = new ServiceCollection();

        IServiceCollection returned = services.AddDietologistModule();

        Assert.Same(services, returned);
        AssertRegistration<IDietologistInvitationRepository, DietologistInvitationRepository>(services);
        AssertRegistration<IRecommendationRepository, RecommendationRepository>(services);
        AssertRegistration<IRecommendationCommentRepository, RecommendationCommentRepository>(services);
        AssertRegistration<IClientTaskRepository, ClientTaskRepository>(services);
        AssertRegistration<IRecommendationTemplateRepository, RecommendationTemplateRepository>(services);
        AssertRegistration<IRecommendationBulkDispatchRepository, RecommendationBulkDispatchRepository>(services);
        AssertRegistration<IAttentionSignalMetricsReadService, AttentionSignalMetricsReadService>(services);

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IDietologistInvitationReadRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IDietologistInvitationReadModelRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IDietologistInvitationWriteRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRecommendationReadRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRecommendationReadModelRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRecommendationWriteRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRecommendationCommentWriteRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRecommendationCommentReadModelRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IClientTaskWriteRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IClientTaskReadModelRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRecommendationTemplateWriteRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRecommendationTemplateReadModelRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRecommendationBulkDispatchLookupRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRecommendationBulkDispatchWriteRepository));
    }

    private static void AssertRegistration<TService, TImplementation>(IServiceCollection services) =>
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(TService) && descriptor.ImplementationType == typeof(TImplementation));
}
