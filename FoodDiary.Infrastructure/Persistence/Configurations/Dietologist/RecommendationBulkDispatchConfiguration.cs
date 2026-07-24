using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Dietologist;

internal sealed class RecommendationBulkDispatchConfiguration : IEntityTypeConfiguration<RecommendationBulkDispatch> {
    public void Configure(EntityTypeBuilder<RecommendationBulkDispatch> builder) {
        builder.Property(dispatch => dispatch.Id)
            .HasConversion(id => id.Value, value => new RecommendationBulkDispatchId(value))
            .ValueGeneratedNever();
        builder.Property(dispatch => dispatch.DietologistUserId)
            .HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(dispatch => dispatch.ClientUserId)
            .HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(dispatch => dispatch.RecommendationId)
            .HasConversion(id => id.Value, value => new RecommendationId(value));
        builder.Property(dispatch => dispatch.IdempotencyKey).IsRequired().HasMaxLength(100);
        builder.HasIndex(dispatch => new {
                dispatch.DietologistUserId,
                dispatch.IdempotencyKey,
                dispatch.ClientUserId,
            })
            .IsUnique();
        builder.HasOne<FoodDiary.Domain.Entities.Users.User>()
            .WithMany()
            .HasForeignKey(dispatch => dispatch.DietologistUserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<FoodDiary.Domain.Entities.Users.User>()
            .WithMany()
            .HasForeignKey(dispatch => dispatch.ClientUserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Recommendation>()
            .WithMany()
            .HasForeignKey(dispatch => dispatch.RecommendationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
