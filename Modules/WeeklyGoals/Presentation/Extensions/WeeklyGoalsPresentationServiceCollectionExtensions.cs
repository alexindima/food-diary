using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class WeeklyGoalsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddWeeklyGoalsPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(WeeklyGoalsPresentationServiceCollectionExtensions).Assembly);
    }
}
