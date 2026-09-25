using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Products.PersistenceModel.Configurations.Products;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product> {
    public void Configure(EntityTypeBuilder<Product> builder) {
        builder.Property<uint>("xmin").IsRowVersion();

        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new ProductId(value));

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.ImageAssetId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ImageAssetId(value.Value) : null);

        builder.OwnsMany(e => e.Images, images => {
            images.ToTable("ProductImages");
            images.WithOwner().HasForeignKey("ProductId");
            images.Property<FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids.ProductId>("ProductId").HasConversion(id => id.Value, value => new FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids.ProductId(value));
            images.Property(e => e.ImageAssetId).HasConversion(id => id.Value, value => new ImageAssetId(value));
            images.Property(e => e.ImageUrl).HasMaxLength(Product.ImageUrlMaxLength);
            images.HasKey("ProductId", nameof(ProductImage.ImageAssetId));
            images.HasIndex(e => e.ImageAssetId);
        });
        builder.Navigation(e => e.Images).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(e => e.Visibility).HasDefaultValue(Visibility.Public);
        builder.Property(e => e.ProductType).HasDefaultValue(ProductType.Unknown);
        builder.HasIndex(e => new { e.UserId, e.CreatedOnUtc });
        builder.HasIndex(e => new { e.Visibility, e.CreatedOnUtc });
        builder.HasIndex(e => e.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
        builder.HasIndex(e => e.Brand)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
        builder.HasIndex(e => e.Category)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
        builder.HasIndex(e => e.Barcode)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

    }
}
