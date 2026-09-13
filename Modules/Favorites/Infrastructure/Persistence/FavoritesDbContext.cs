using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Favorites.Infrastructure.Persistence;

public sealed class FavoritesDbContext(DbContextOptions<FavoritesDbContext> options) : DbContext(options) {
    public DbSet<FavoriteMeal> FavoriteMeals => Set<FavoriteMeal>();
    public DbSet<FavoriteProduct> FavoriteProducts => Set<FavoriteProduct>();
    public DbSet<FavoriteRecipe> FavoriteRecipes => Set<FavoriteRecipe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyFavoritesPersistenceModel();
}
