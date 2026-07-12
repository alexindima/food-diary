using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.ContentReports;


internal sealed class ContentReportConfiguration : IEntityTypeConfiguration<ContentReport> {
    public void Configure(EntityTypeBuilder<ContentReport> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new ContentReportId(value));

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.AdminNote).HasMaxLength(2000);

        builder.Property(e => e.TargetType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(e => new { e.Status, e.CreatedOnUtc });
        builder.HasIndex(e => new { e.UserId, e.TargetType, e.TargetId });

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
