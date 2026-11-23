using FoodDiary.Modules.Admin.Presentation.Mappings;
using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Admin.Presentation.Extensions;

public static class AdminPresentationServiceCollectionExtensions {
    public static IServiceCollection AddAdminPresentation(this IServiceCollection services) {
        services.AddScoped<AdminRecipeModerationHttpProcessor>();
        return services.AddPresentationAssembly(typeof(AdminPresentationServiceCollectionExtensions).Assembly);
    }
}
