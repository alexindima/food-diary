using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Ai.PersistenceModel.Configurations;

internal sealed class FoodRecognitionJobImageConfiguration : IEntityTypeConfiguration<FoodRecognitionJobImage> {
    public void Configure(EntityTypeBuilder<FoodRecognitionJobImage> builder) {
        builder.ToTable("FoodRecognitionJobImages");
        builder.HasKey(x => new { x.JobId, x.ImageAssetId });
        builder.Property(x => x.ImageAssetId).HasConversion(id => id.Value, value => new ImageAssetId(value));
        builder.Property(x => x.ImageUrl).HasMaxLength(2048);
        builder.HasIndex(x => new { x.JobId, x.Position }).IsUnique();
        builder.HasOne<FoodRecognitionJob>().WithMany(x => x.AdditionalImages).HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
    }
}
