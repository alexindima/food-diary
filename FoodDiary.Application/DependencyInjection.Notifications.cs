using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Ai.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application;

public static partial class DependencyInjection {
    private static void AddCommunicationServices(this IServiceCollection services) {
        services.AddScoped<IOpenAiFoodService, OpenAiFoodService>();
    }
}
