using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Admin.Presentation.Extensions;

public static class AdminPresentationServiceCollectionExtensions {
    public static IServiceCollection AddAdminPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(AdminPresentationServiceCollectionExtensions).Assembly);
    }
}
