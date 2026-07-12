using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Dietologist;


internal sealed class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation> {
    public void Configure(EntityTypeBuilder<Recommendation> builder) {
        builder.Property<uint>("xmin").IsRowVersion();

        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new RecommendationId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.DietologistUserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.ClientUserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.Text)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasOne(e => e.DietologistUser)
            .WithMany()
            .HasForeignKey(e => e.DietologistUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ClientUser)
            .WithMany()
            .HasForeignKey(e => e.ClientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ClientUserId);
        builder.HasIndex(e => new { e.DietologistUserId, e.ClientUserId });
    }
}
