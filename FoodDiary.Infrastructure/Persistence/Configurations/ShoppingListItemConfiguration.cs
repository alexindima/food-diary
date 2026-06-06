using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class ShoppingListItemConfiguration : IEntityTypeConfiguration<ShoppingListItem> {
    public void Configure(EntityTypeBuilder<ShoppingListItem> builder) {
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new ShoppingListItemId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.ShoppingListId).HasConversion(
            id => id.Value,
            value => new ShoppingListId(value));

        builder.Property(e => e.ProductId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ProductId(value.Value) : null);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Category)
            .HasMaxLength(128);

        builder.Property(e => e.IsChecked)
            .HasDefaultValue(value: false);

        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
