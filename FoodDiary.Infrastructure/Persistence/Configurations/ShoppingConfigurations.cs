using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class ShoppingListConfiguration : IEntityTypeConfiguration<ShoppingList> {
    public void Configure(EntityTypeBuilder<ShoppingList> entity) {
        entity.Property<uint>("xmin").IsRowVersion();

        entity.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new ShoppingListId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(128);

        entity.HasOne(e => e.User)
            .WithMany(u => u.ShoppingLists)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.Items)
            .WithOne(i => i.ShoppingList)
            .HasForeignKey(i => i.ShoppingListId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.Navigation(e => e.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ShoppingListItemConfiguration : IEntityTypeConfiguration<ShoppingListItem> {
    public void Configure(EntityTypeBuilder<ShoppingListItem> entity) {
        entity.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new ShoppingListItemId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.ShoppingListId).HasConversion(
            id => id.Value,
            value => new ShoppingListId(value));

        entity.Property(e => e.ProductId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ProductId(value.Value) : null);

        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(256);

        entity.Property(e => e.Category)
            .HasMaxLength(128);

        entity.Property(e => e.IsChecked)
            .HasDefaultValue(false);

        entity.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        entity.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
