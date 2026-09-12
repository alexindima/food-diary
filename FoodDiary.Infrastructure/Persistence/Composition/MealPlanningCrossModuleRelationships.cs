using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Composition;

internal static class MealPlanningCrossModuleRelationships {
    internal static void Configure(ModelBuilder modelBuilder) {
        modelBuilder.Entity<MealPlan>().HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        modelBuilder.Entity<MealPlanMeal>().HasOne<Recipe>()
            .WithMany()
            .HasForeignKey(e => e.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ShoppingList>().HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShoppingListItem>().HasOne<Product>()
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
