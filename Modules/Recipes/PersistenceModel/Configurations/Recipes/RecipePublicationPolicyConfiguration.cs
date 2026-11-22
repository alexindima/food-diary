using FoodDiary.Modules.Recipes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Recipes.PersistenceModel.Configurations.Recipes;

internal sealed class RecipePublicationPolicyConfiguration : IEntityTypeConfiguration<RecipePublicationPolicy> {
    public void Configure(EntityTypeBuilder<RecipePublicationPolicy> builder) {
        builder.ToTable("RecipePublicationPolicies");
        builder.HasKey(policy => policy.Id);
        builder.Property(policy => policy.Id).ValueGeneratedNever();
        builder.Property(policy => policy.RulesJson).HasColumnType("jsonb");
        builder.HasIndex(policy => policy.PublishedAt);
    }
}
