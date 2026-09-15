using FoodDiary.Modules.Recipes.PersistenceModel;
using FoodDiary.Modules.Recipes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Recipes.Infrastructure.Persistence;

public sealed class RecipesDbContext(DbContextOptions<RecipesDbContext> options) : DbContext(options) {
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyRecipesPersistenceModel();
}
