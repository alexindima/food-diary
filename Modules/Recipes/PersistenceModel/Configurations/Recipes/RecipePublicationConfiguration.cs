using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Recipes.PersistenceModel.Configurations.Recipes;

internal sealed class RecipePublicationConfiguration : IEntityTypeConfiguration<RecipePublicationRevision> {
    public void Configure(EntityTypeBuilder<RecipePublicationRevision> builder) {
        builder.ToTable("RecipePublicationRevisions");
        builder.HasKey(revision => revision.Id);
        builder.Property(revision => revision.Id).ValueGeneratedNever();
        builder.Property(revision => revision.RecipeId).HasConversion(id => id.Value, value => (RecipeId)value);
        builder.Property(revision => revision.Status).HasMaxLength(24);
        builder.Property(revision => revision.Reason).HasMaxLength(2000);
        builder.Property(revision => revision.ChangeJson).HasColumnType("jsonb");
        builder.Property(revision => revision.ContentJson).HasColumnType("jsonb");
        builder.Property(revision => revision.PrimaryResult).HasColumnType("jsonb");
        builder.Property(revision => revision.ShadowResult).HasColumnType("jsonb");
        builder.Property<uint>("xmin").IsRowVersion();
        builder.HasIndex(revision => revision.RecipeId).IsUnique().HasFilter("\"IsCurrent\"");
        builder.HasIndex(revision => revision.OwnerId);
        builder.HasIndex(revision => new { revision.Status, revision.CreatedAt });
        builder.HasIndex(revision => revision.NextAttemptAt);
        builder.HasIndex(revision => revision.ShadowNextAttemptAt);
        builder.HasOne<Recipe>().WithMany().HasForeignKey(revision => revision.RecipeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(revision => revision.Images).WithOne().HasForeignKey(image => image.RevisionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(revision => revision.Images).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
