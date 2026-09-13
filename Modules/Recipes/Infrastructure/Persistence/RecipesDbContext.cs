using FoodDiary.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public sealed class RecipesDbContext(DbContextOptions<RecipesDbContext> options) : DbContext(options) {
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyRecipesPersistenceModel();
}
