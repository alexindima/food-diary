using FoodDiary.Modules.Recipes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Recipes.PersistenceModel.Configurations.Recipes;

internal sealed class RecipeModerationBudgetConfiguration : IEntityTypeConfiguration<RecipeModerationBudget> {
    public void Configure(EntityTypeBuilder<RecipeModerationBudget> builder) {
        builder.ToTable("RecipeModerationBudgets");
        builder.HasKey(budget => new { budget.Provider, budget.WindowStart, budget.WindowSeconds });
        builder.Property(budget => budget.Provider).HasMaxLength(16);
    }
}
