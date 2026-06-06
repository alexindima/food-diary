using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class AiUsageConfiguration : IEntityTypeConfiguration<AiUsage> {
    public void Configure(EntityTypeBuilder<AiUsage> builder) {
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new AiUsageId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.Operation)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.Model)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasOne<global::FoodDiary.Domain.Entities.Users.User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.CreatedOnUtc });
    }
}
