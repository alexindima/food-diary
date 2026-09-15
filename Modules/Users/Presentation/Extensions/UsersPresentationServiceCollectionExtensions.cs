using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Users.Presentation.Extensions;

public static class UsersPresentationServiceCollectionExtensions {
    public static IServiceCollection AddUsersPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(UsersPresentationServiceCollectionExtensions).Assembly);
    }
}
