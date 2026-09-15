using FoodDiary.Modules.Marketing.Application.Commands.RecordPremiumConversion;
using FoodDiary.Modules.Marketing.Contracts.Commands.RecordPremiumConversion;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Marketing.Application.Tests;

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
