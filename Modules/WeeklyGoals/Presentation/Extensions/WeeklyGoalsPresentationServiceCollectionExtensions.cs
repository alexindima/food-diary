using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.WeeklyGoals.Presentation.Extensions;

public static class WeeklyGoalsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddWeeklyGoalsPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(WeeklyGoalsPresentationServiceCollectionExtensions).Assembly);
    }
}
