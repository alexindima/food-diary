using FluentValidation;
using FoodDiary.Application.RecipeCommunity.RecipeComments.Common;
using FoodDiary.Application.RecipeCommunity.RecipeComments.Services;
using FoodDiary.Application.RecipeCommunity.RecipeLikes.Common;
using FoodDiary.Application.RecipeCommunity.RecipeLikes.Services;
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
