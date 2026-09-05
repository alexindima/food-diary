using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class WeeklyCheckInPresentationServiceCollectionExtensions {
    public static IServiceCollection AddWeeklyCheckInPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(WeeklyCheckInPresentationServiceCollectionExtensions).Assembly);
    }
}
