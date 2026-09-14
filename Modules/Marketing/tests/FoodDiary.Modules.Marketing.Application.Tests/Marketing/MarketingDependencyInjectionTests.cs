using FoodDiary.Application.Marketing;
using FoodDiary.Application.Marketing.Commands.RecordPremiumConversion;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Tests.Marketing;

[ExcludeFromCodeCoverage]
public sealed class MarketingDependencyInjectionTests {
    [Fact]
    public void AddMarketingApplication_RegistersOneOwnerConversionHandlerAndSender() {
        var services = new ServiceCollection();
        services.AddMarketingApplication();
        ServiceDescriptor handler = Assert.Single(services,
            descriptor => descriptor.ServiceType == typeof(IRequestHandler<RecordPremiumConversionCommand, Unit>));
        Assert.Equal(typeof(RecordPremiumConversionCommandHandler), handler.ImplementationType);
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ISender));
    }
}
