using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Recipes.PersistenceModel.Configurations.Recipes;

internal sealed class RecipePublicationImageReferenceConfiguration : IEntityTypeConfiguration<RecipePublicationImageReference> {
    public void Configure(EntityTypeBuilder<RecipePublicationImageReference> builder) {
        builder.ToTable("RecipePublicationImageReferences");
        builder.HasKey(image => new { image.RevisionId, image.AssetId });
        builder.HasIndex(image => image.AssetId);
        builder.Property(image => image.AssetId).HasConversion(id => id.Value, value => (ImageAssetId)value);
    }
}
