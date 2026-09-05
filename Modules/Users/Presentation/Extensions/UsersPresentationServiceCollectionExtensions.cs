using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class UsersPresentationServiceCollectionExtensions {
    public static IServiceCollection AddUsersPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(UsersPresentationServiceCollectionExtensions).Assembly);
    }
}
