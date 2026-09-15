using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.WeeklyCheckIn.Presentation.Extensions;

public static class WeeklyCheckInPresentationServiceCollectionExtensions {
    public static IServiceCollection AddWeeklyCheckInPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(WeeklyCheckInPresentationServiceCollectionExtensions).Assembly);
    }
}
