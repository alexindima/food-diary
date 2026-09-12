using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Usda;
using FoodDiary.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Composition;

internal static class ProductsCrossModuleRelationships {
    internal static void Configure(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Product>().HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(e => e.ImageAssetId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.ClientNoAction);

        modelBuilder.Entity<Product>().HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId);

        modelBuilder.Entity<Product>().HasOne<UsdaFood>()
            .WithMany()
            .HasForeignKey(e => e.UsdaFdcId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
