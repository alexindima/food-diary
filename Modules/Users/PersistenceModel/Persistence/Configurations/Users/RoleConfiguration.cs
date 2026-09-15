using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Users.PersistenceModel.Persistence.Configurations.Users;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role> {
    public void Configure(EntityTypeBuilder<Role> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new RoleId(value));

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(e => e.Name)
            .IsUnique();

        builder.Metadata.FindNavigation(nameof(Role.UserRoles))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
