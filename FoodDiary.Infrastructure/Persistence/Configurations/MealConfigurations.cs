using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class MealConfiguration : IEntityTypeConfiguration<Meal> {
    public void Configure(EntityTypeBuilder<Meal> entity) {
        entity.Property<uint>("xmin").IsRowVersion();

        entity.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new MealId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.ImageAssetId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ImageAssetId(value.Value) : null);

        entity.HasOne(e => e.User)
            .WithMany(u => u.Meals)
            .HasForeignKey(e => e.UserId);
        entity.Property(e => e.IsNutritionAutoCalculated).HasDefaultValue(true);

        entity.HasIndex(e => new { e.UserId, e.Date, e.CreatedOnUtc });

        entity.HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(e => e.ImageAssetId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasMany(e => e.AiSessions)
            .WithOne(s => s.Meal)
            .HasForeignKey(s => s.MealId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.Navigation(e => e.AiSessions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class MealItemConfiguration : IEntityTypeConfiguration<MealItem> {
    public void Configure(EntityTypeBuilder<MealItem> entity) {
        entity.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new MealItemId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.MealId).HasConversion(
            id => id.Value,
            value => new MealId(value));

        entity.Property(e => e.ProductId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ProductId(value.Value) : null);

        entity.Property(e => e.RecipeId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new RecipeId(value.Value) : null);

        entity.HasOne(e => e.Meal)
            .WithMany(m => m.Items)
            .HasForeignKey(e => e.MealId);

        entity.HasOne(e => e.Product)
            .WithMany(p => p.MealItems)
            .HasForeignKey(e => e.ProductId)
            .IsRequired(false);

        entity.HasOne(e => e.Recipe)
            .WithMany(r => r.MealItems)
            .HasForeignKey(e => e.RecipeId)
            .IsRequired(false);
    }
}

internal sealed class MealAiSessionConfiguration : IEntityTypeConfiguration<MealAiSession> {
    public void Configure(EntityTypeBuilder<MealAiSession> entity) {
        entity.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new MealAiSessionId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.MealId).HasConversion(
            id => id.Value,
            value => new MealId(value));

        entity.Property(e => e.ImageAssetId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ImageAssetId(value.Value) : null);

        entity.HasOne(e => e.ImageAsset)
            .WithMany()
            .HasForeignKey(e => e.ImageAssetId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.Property(e => e.RecognizedAtUtc)
            .HasColumnType("timestamp with time zone");

        entity.Property(e => e.Notes)
            .HasMaxLength(2048);

        entity.HasMany(e => e.Items)
            .WithOne(i => i.Session)
            .HasForeignKey(i => i.MealAiSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.Navigation(e => e.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class MealAiItemConfiguration : IEntityTypeConfiguration<MealAiItem> {
    public void Configure(EntityTypeBuilder<MealAiItem> entity) {
        entity.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new MealAiItemId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.MealAiSessionId).HasConversion(
            id => id.Value,
            value => new MealAiSessionId(value));

        entity.Property(e => e.NameEn)
            .IsRequired()
            .HasMaxLength(256);

        entity.Property(e => e.NameLocal)
            .HasMaxLength(256);

        entity.Property(e => e.Unit)
            .IsRequired()
            .HasMaxLength(32);
    }
}
