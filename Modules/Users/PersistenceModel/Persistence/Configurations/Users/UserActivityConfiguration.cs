using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Users.PersistenceModel.Persistence.Configurations.Users;

internal sealed class UserActivityConfiguration : IEntityTypeConfiguration<UserActivity> {
    public void Configure(EntityTypeBuilder<UserActivity> builder) {
        builder.ToTable("UserActivities");
        builder.HasKey(activity => activity.UserId);
        builder.Property(activity => activity.UserId).HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(activity => activity.LastActiveAtUtc).HasColumnType("timestamp with time zone");
        builder.HasIndex(activity => activity.LastActiveAtUtc);
        builder.HasOne<User>().WithOne(user => user.Activity).HasForeignKey<UserActivity>(activity => activity.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
