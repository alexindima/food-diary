using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Composition;

internal static class ProductsCrossModuleRelationships {
    internal static void Configure(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Product>().HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(e => e.ImageAssetId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.ClientNoAction);

        modelBuilder.Entity<Product>().OwnsMany(product => product.Images, images =>
            images.HasOne<ImageAsset>().WithMany().HasForeignKey(image => image.ImageAssetId).OnDelete(DeleteBehavior.ClientNoAction));

        modelBuilder.Entity<Product>().HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId);

        // FDC identifiers can refer to live branded foods outside the locally seeded SR Legacy subset.
        // The USDA owner validates these external identifiers before linking them to an owned product.
    }
}
