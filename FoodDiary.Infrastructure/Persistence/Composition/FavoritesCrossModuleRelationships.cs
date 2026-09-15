using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteMeals;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteProducts;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteRecipes;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Composition;

internal static class FavoritesCrossModuleRelationships {
    internal static void Configure(ModelBuilder modelBuilder) {
        modelBuilder.Entity<FavoriteMeal>().HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FavoriteMeal>().HasOne<Meal>()
            .WithMany()
            .HasForeignKey(e => e.MealId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FavoriteProduct>().HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FavoriteProduct>().HasOne<Product>()
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FavoriteRecipe>().HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FavoriteRecipe>().HasOne<Recipe>()
            .WithMany()
            .HasForeignKey(e => e.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
