using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Composition;

internal static class RecipesCrossModuleRelationships {
    internal static void Configure(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Recipe>().HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId);

        modelBuilder.Entity<Recipe>().HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(e => e.ImageAssetId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.ClientNoAction);

        modelBuilder.Entity<RecipeIngredient>().HasOne<Product>()
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .IsRequired(false);

        modelBuilder.Entity<RecipeStep>().HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(e => e.ImageAssetId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.ClientNoAction);
    }
}
