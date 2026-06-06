using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class CycleConfiguration : IEntityTypeConfiguration<Cycle> {
    public void Configure(EntityTypeBuilder<Cycle> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new CycleId(value));

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.StartDate)
            .HasColumnType("date");

        builder.Property(e => e.AverageLength)
            .HasDefaultValue(28);

        builder.Property(e => e.LutealLength)
            .HasDefaultValue(14);

        builder.Property(e => e.Notes)
            .HasMaxLength(1024);

        builder.HasOne<global::FoodDiary.Domain.Entities.Users.User>()
            .WithMany(u => u.Cycles)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Days)
            .WithOne(d => d.Cycle)
            .HasForeignKey(d => d.CycleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Days)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => new { e.UserId, e.StartDate })
            .HasDatabaseName("IX_Cycles_User_StartDate");
    }
}
