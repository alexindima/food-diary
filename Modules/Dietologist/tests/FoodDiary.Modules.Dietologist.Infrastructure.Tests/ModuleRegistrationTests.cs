using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Dietologist.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class ModuleRegistrationTests {
    [Fact]
    public void AddDietologistModule_RegistersEveryOwnedPersistencePort() {
        var services = new ServiceCollection();

        IServiceCollection returned = services.AddDietologistModule();

        Assert.Same(services, returned);
        AssertRegistration<IDietologistInvitationRepository, DietologistInvitationRepository>(services);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRecommendationRepository) && descriptor.ImplementationFactory is not null);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRecommendationCommentRepository) && descriptor.ImplementationFactory is not null);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IClientTaskRepository) && descriptor.ImplementationFactory is not null);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRecommendationTemplateRepository) && descriptor.ImplementationFactory is not null);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRecommendationBulkDispatchRepository) && descriptor.ImplementationFactory is not null);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IAttentionSignalMetricsReadService));

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IDietologistInvitationReadRepository));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IDietologistInvitationReadModelRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IDietologistInvitationWriteRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRecommendationReadRepository));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IRecommendationReadModelRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRecommendationWriteRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRecommendationCommentWriteRepository));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IRecommendationCommentReadModelRepository));
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
