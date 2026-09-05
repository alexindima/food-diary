using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class MarketingPresentationServiceCollectionExtensions {
    public static IServiceCollection AddMarketingPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(MarketingPresentationServiceCollectionExtensions).Assembly);
    }
}
