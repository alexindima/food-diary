using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Recipes.PersistenceModel.Configurations.Recipes;

internal sealed class RecipeStepConfiguration : IEntityTypeConfiguration<RecipeStep> {
    public void Configure(EntityTypeBuilder<RecipeStep> builder) {
        builder.OwnsMany(e => e.Images, images => {
            images.ToTable("RecipeStepImages");
            images.WithOwner().HasForeignKey("RecipeStepId");
            images.Property<RecipeStepId>("RecipeStepId").HasConversion(id => id.Value, value => new RecipeStepId(value));
            images.Property(e => e.ImageAssetId).HasConversion(id => id.Value, value => new ImageAssetId(value));
            images.Property(e => e.ImageUrl).HasMaxLength(Recipe.ImageUrlMaxLength);
            images.HasKey("RecipeStepId", nameof(RecipeImage.ImageAssetId));
            images.HasIndex(e => e.ImageAssetId);
        });
        builder.Navigation(e => e.Images).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new RecipeStepId(value));

        builder.Property(e => e.RecipeId).HasConversion(
            id => id.Value,
            value => new RecipeId(value));

        builder.HasOne(e => e.Recipe)
            .WithMany(r => r.Steps)
            .HasForeignKey(e => e.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.ImageAssetId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ImageAssetId(value.Value) : null);

    }
}
