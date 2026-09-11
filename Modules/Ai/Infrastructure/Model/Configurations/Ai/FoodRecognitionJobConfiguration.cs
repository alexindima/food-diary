using FoodDiary.Infrastructure.Persistence.Ai;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Ai;

internal sealed class FoodRecognitionJobConfiguration : IEntityTypeConfiguration<FoodRecognitionJob> {
    public void Configure(EntityTypeBuilder<FoodRecognitionJob> builder) {
        builder.ToTable("FoodRecognitionJobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(x => x.ImageAssetId).HasConversion(id => id.Value, value => new ImageAssetId(value));
        builder.Property(x => x.ImageUrl).HasMaxLength(2048);
        builder.Property(x => x.Description).HasMaxLength(2048);
        builder.Property(x => x.Status).HasMaxLength(16);
        builder.Property(x => x.ErrorCode).HasMaxLength(128);
        builder.Property(x => x.NutritionErrorCode).HasMaxLength(128);
        builder.HasIndex(x => new { x.Status, x.CreatedOnUtc });
        builder.HasIndex(x => new { x.UserId, x.CreatedOnUtc });
        builder.HasIndex(x => x.UpdatedOnUtc);
    }
}
