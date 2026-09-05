using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class NotificationsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddNotificationsPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(NotificationsPresentationServiceCollectionExtensions).Assembly);
    }
}
