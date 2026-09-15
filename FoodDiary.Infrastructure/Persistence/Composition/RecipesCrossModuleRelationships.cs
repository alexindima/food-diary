using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
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
