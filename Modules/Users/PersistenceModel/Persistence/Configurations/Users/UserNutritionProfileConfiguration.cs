using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Users.PersistenceModel.Persistence.Configurations.Users;

internal sealed class UserNutritionProfileConfiguration : IEntityTypeConfiguration<UserNutritionProfile> {
    public void Configure(EntityTypeBuilder<UserNutritionProfile> builder) {
        builder.ToTable("UserNutritionProfiles");
        builder.HasKey(state => state.Id);
        builder.Property(state => state.Id).HasConversion(id => id.Value, value => new UserId(value)).ValueGeneratedNever();
        builder.Property<uint>("xmin").IsRowVersion();
        builder.Property(state => state.ActivityLevel).HasConversion<string>();
    }
}
