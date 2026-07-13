using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Marketing;
using FoodDiary.Application.Marketing.Common;
using FoodDiary.Application.Marketing.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Tests.Marketing;

[ExcludeFromCodeCoverage]
public sealed class MarketingDependencyInjectionTests {
    [Fact]
    public void AddMarketingModule_ResolvesConversionAliasesToSameScopedService() {
        var services = new ServiceCollection();
        services.AddMarketingModule();
        var recorder = new MarketingConversionRecorder(
            Substitute.For<IMarketingAttributionEventReadRepository>(),
            Substitute.For<IMarketingAttributionEventWriteRepository>(),
            TimeProvider.System);
        var provider = new FixedServiceProvider(recorder);

        ServiceDescriptor marketingDescriptor = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IMarketingConversionRecorder));
        ServiceDescriptor billingDescriptor = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IBillingMarketingConversionRecorder));
        object? marketingRecorder = marketingDescriptor.ImplementationFactory!(provider);
        object? billingRecorder = billingDescriptor.ImplementationFactory!(provider);

        Assert.Multiple(
            () => Assert.Same(recorder, marketingRecorder),
            () => Assert.Same(marketingRecorder, billingRecorder));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedServiceProvider(MarketingConversionRecorder recorder) : IServiceProvider {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(MarketingConversionRecorder) ? recorder : null;
    }
}
