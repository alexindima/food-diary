using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class DietologistInvitationConfiguration : IEntityTypeConfiguration<DietologistInvitation> {
    public void Configure(EntityTypeBuilder<DietologistInvitation> builder) {
        builder.Property<uint>("xmin").IsRowVersion();

        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new DietologistInvitationId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.ClientUserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.DietologistUserId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new UserId(value.Value) : null);

        builder.Property(e => e.DietologistEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.TokenHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasOne(e => e.ClientUser)
            .WithMany()
            .HasForeignKey(e => e.ClientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.DietologistUser)
            .WithMany()
            .HasForeignKey(e => e.DietologistUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.ClientUserId);
        builder.HasIndex(e => e.DietologistUserId);
        builder.HasIndex(e => new { e.ClientUserId, e.Status });
        builder.HasIndex(e => new { e.DietologistEmail, e.Status });
    }
}
