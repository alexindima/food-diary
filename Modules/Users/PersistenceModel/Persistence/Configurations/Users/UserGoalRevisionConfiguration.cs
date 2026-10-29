using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Users.PersistenceModel.Persistence.Configurations.Users;

internal sealed class UserGoalRevisionConfiguration : IEntityTypeConfiguration<UserGoalRevision> {
    public void Configure(EntityTypeBuilder<UserGoalRevision> builder) {
        builder.ToTable("UserGoalHistory");
        builder.HasKey(revision => revision.Id);
        builder.Property(revision => revision.Id).HasConversion(id => id.Value, value => new UserGoalRevisionId(value)).ValueGeneratedNever();
        builder.Property(revision => revision.UserId).HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(revision => revision.Revision).ValueGeneratedNever();
        builder.HasIndex(revision => new { revision.UserId, revision.Revision });
        builder.HasIndex(revision => new { revision.UserId, revision.EffectiveFrom, revision.Revision });
    }
}
