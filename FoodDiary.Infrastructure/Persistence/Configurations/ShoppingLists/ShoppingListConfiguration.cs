using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.ShoppingLists;


internal sealed class ShoppingListConfiguration : IEntityTypeConfiguration<ShoppingList> {
    public void Configure(EntityTypeBuilder<ShoppingList> builder) {
        builder.Property<uint>("xmin").IsRowVersion();

        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new ShoppingListId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasOne(e => e.User)
            .WithMany(u => u.ShoppingLists)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Items)
            .WithOne(i => i.ShoppingList)
            .HasForeignKey(i => i.ShoppingListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
