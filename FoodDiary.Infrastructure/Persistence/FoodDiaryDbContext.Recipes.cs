using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteRecipes;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Recipes;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Social;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public sealed partial class FoodDiaryDbContext {
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<FavoriteRecipe> FavoriteRecipes => Set<FavoriteRecipe>();
    public DbSet<RecipeComment> RecipeComments => Set<RecipeComment>();
    public DbSet<RecipeLike> RecipeLikes => Set<RecipeLike>();
}
