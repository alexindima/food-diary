using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
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
