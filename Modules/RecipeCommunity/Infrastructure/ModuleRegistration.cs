using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Application.RecipeCommunity;
using FoodDiary.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Application.Abstractions.RecipeLikes.Common;
using FoodDiary.Infrastructure.Persistence.RecipeComments;
using FoodDiary.Infrastructure.Persistence.RecipeLikes;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddRecipeCommunityModule(this IServiceCollection services) {
        services.AddRecipeCommunityApplication();
        services.AddScoped(static provider => provider.GetRequiredService<FoodDiaryDbContext>()
            .CreateModuleContext<RecipeCommunityDbContext>(static options => new RecipeCommunityDbContext(options)));
        services.AddScoped<IRecipeCommentRepository>(static provider => new RecipeCommentRepository(
            provider.GetRequiredService<RecipeCommunityDbContext>().RecipeComments, provider.GetRequiredService<IUserCommentAuthorReadService>()));
        services.AddScoped<IRecipeCommentReadRepository>(static provider => provider.GetRequiredService<IRecipeCommentRepository>());
        services.AddScoped<IRecipeCommentReadModelRepository>(static provider => provider.GetRequiredService<IRecipeCommentRepository>());
        services.AddScoped<IRecipeCommentWriteRepository>(static provider => provider.GetRequiredService<IRecipeCommentRepository>());
        services.AddScoped<IRecipeLikeRepository>(static provider => new RecipeLikeRepository(
            provider.GetRequiredService<RecipeCommunityDbContext>().RecipeLikes));
        services.AddScoped<IRecipeLikeReadRepository>(static provider => provider.GetRequiredService<IRecipeLikeRepository>());
        services.AddScoped<IRecipeLikeWriteRepository>(static provider => provider.GetRequiredService<IRecipeLikeRepository>());

        return services;
    }
}
