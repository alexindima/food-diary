using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class MealAiSessionConfiguration : IEntityTypeConfiguration<MealAiSession> {
    public void Configure(EntityTypeBuilder<MealAiSession> builder) {
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new MealAiSessionId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.MealId).HasConversion(
            id => id.Value,
            value => new MealId(value));

        builder.Property(e => e.ImageAssetId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ImageAssetId(value.Value) : null);

        builder.HasOne(e => e.ImageAsset)
            .WithMany()
            .HasForeignKey(e => e.ImageAssetId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.Source)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasDefaultValue(AiRecognitionSource.Text);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasDefaultValue(MealAiSessionStatus.Reviewed)
            .HasSentinel((MealAiSessionStatus)0);

        builder.Property(e => e.RecognizedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.Notes)
            .HasMaxLength(2048);

        builder.HasMany(e => e.Items)
            .WithOne(i => i.Session)
            .HasForeignKey(i => i.MealAiSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
