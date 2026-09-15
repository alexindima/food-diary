using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Marketing.Presentation.Extensions;

public static class MarketingPresentationServiceCollectionExtensions {
    public static IServiceCollection AddMarketingPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(MarketingPresentationServiceCollectionExtensions).Assembly);
    }
}
