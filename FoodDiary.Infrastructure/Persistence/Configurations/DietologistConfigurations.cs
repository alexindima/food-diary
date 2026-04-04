using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class DietologistInvitationConfiguration : IEntityTypeConfiguration<DietologistInvitation> {
    public void Configure(EntityTypeBuilder<DietologistInvitation> entity) {
        entity.Property<uint>("xmin").IsRowVersion();

        entity.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new DietologistInvitationId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.ClientUserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.DietologistUserId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new UserId(value.Value) : null);

        entity.Property(e => e.DietologistEmail)
            .IsRequired()
            .HasMaxLength(256);

        entity.Property(e => e.TokenHash)
            .IsRequired()
            .HasMaxLength(256);

        entity.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.HasOne(e => e.ClientUser)
            .WithMany()
            .HasForeignKey(e => e.ClientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.DietologistUser)
            .WithMany()
            .HasForeignKey(e => e.DietologistUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasIndex(e => e.ClientUserId);
        entity.HasIndex(e => e.DietologistUserId);
        entity.HasIndex(e => new { e.ClientUserId, e.Status });
        entity.HasIndex(e => new { e.DietologistEmail, e.Status });
    }
}

internal sealed class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation> {
    public void Configure(EntityTypeBuilder<Recommendation> entity) {
        entity.Property<uint>("xmin").IsRowVersion();

        entity.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new RecommendationId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.DietologistUserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.ClientUserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.Text)
            .IsRequired()
            .HasMaxLength(2000);

        entity.HasOne(e => e.DietologistUser)
            .WithMany()
            .HasForeignKey(e => e.DietologistUserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.ClientUser)
            .WithMany()
            .HasForeignKey(e => e.ClientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(e => e.ClientUserId);
        entity.HasIndex(e => new { e.DietologistUserId, e.ClientUserId });
    }
}
