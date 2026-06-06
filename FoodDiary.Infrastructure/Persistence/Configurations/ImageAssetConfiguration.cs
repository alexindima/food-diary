using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class ImageAssetConfiguration : IEntityTypeConfiguration<ImageAsset> {
    public void Configure(EntityTypeBuilder<ImageAsset> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new ImageAssetId(value));

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.ObjectKey).IsRequired();
        builder.Property(e => e.Url).IsRequired();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
