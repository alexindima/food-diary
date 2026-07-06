using FoodDiary.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Application.Abstractions.RecipeLikes.Common;
using FoodDiary.Infrastructure.Persistence.RecipeComments;
using FoodDiary.Infrastructure.Persistence.RecipeLikes;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddRecipeInteractionPersistence(this IServiceCollection services) {
        services.AddScoped<IRecipeCommentRepository, RecipeCommentRepository>();
        services.AddScoped<IRecipeCommentReadRepository>(static provider => provider.GetRequiredService<IRecipeCommentRepository>());
        services.AddScoped<IRecipeCommentReadModelRepository>(static provider => provider.GetRequiredService<IRecipeCommentRepository>());
        services.AddScoped<IRecipeCommentWriteRepository>(static provider => provider.GetRequiredService<IRecipeCommentRepository>());
        services.AddScoped<IRecipeLikeRepository, RecipeLikeRepository>();
        services.AddScoped<IRecipeLikeReadRepository>(static provider => provider.GetRequiredService<IRecipeLikeRepository>());
        services.AddScoped<IRecipeLikeWriteRepository>(static provider => provider.GetRequiredService<IRecipeLikeRepository>());

        return services;
    }
}
