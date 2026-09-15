using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Composition;

internal static class MealsCrossModuleRelationships {
    internal static void Configure(ModelBuilder modelBuilder) {
        modelBuilder.Entity<MealAiSession>().HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(e => e.ImageAssetId)
            .OnDelete(DeleteBehavior.ClientNoAction);

        modelBuilder.Entity<Meal>().HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId);

        modelBuilder.Entity<Meal>().HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(e => e.ImageAssetId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.ClientNoAction);

        modelBuilder.Entity<MealItem>().HasOne<FoodDiary.Modules.Products.Domain.Entities.Product>()
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .IsRequired(false);

        modelBuilder.Entity<MealItem>().HasOne<Recipe>()
            .WithMany()
            .HasForeignKey(e => e.RecipeId)
            .IsRequired(false);

        modelBuilder.Entity<MealRecognitionReceipt>().HasOne<User>().WithMany().HasForeignKey(receipt => receipt.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
