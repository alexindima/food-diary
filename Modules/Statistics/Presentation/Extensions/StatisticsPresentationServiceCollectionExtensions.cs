using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Statistics.Presentation.Extensions;

public static class StatisticsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddStatisticsPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(StatisticsPresentationServiceCollectionExtensions).Assembly);
    }
}
