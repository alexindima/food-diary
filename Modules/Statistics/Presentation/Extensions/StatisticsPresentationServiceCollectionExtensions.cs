using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class StatisticsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddStatisticsPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(StatisticsPresentationServiceCollectionExtensions).Assembly);
    }
}
