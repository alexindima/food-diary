using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Images.PersistenceModel.Configurations;

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
        builder.Property(e => e.IsConfirmed);
    }
}
