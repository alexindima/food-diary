using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Ai.Services;
using FoodDiary.Application.Images.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application;

public static partial class DependencyInjection {
    private static void AddCommunicationServices(this IServiceCollection services) {
        services.AddScoped<IImageAssetAccessService, ImageAssetAccessService>();
        services.AddScoped<IImageAssetCleanupService, ImageAssetCleanupService>();
        services.AddScoped<IOpenAiFoodService, OpenAiFoodService>();
    }
}
