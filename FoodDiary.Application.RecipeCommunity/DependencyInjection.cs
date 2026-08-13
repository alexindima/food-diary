using FluentValidation;
using FoodDiary.Application.RecipeComments.Common;
using FoodDiary.Application.RecipeComments.Services;
using FoodDiary.Application.RecipeLikes.Common;
using FoodDiary.Application.RecipeLikes.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.RecipeCommunity;

public static class DependencyInjection {
    public static IServiceCollection AddRecipeCommunityModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<IRecipeCommentReadService, RecipeCommentReadService>();
        services.AddScoped<IRecipeLikeReadService, RecipeLikeReadService>();

        return services;
    }
}
