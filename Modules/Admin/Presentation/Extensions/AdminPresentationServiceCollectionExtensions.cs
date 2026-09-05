using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class AdminPresentationServiceCollectionExtensions {
    public static IServiceCollection AddAdminPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(AdminPresentationServiceCollectionExtensions).Assembly);
    }
}
