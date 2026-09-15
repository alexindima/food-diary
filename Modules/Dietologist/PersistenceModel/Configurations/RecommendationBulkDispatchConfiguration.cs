using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Dietologist.PersistenceModel.Configurations;

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
        builder.HasOne<Recommendation>()
            .WithMany()
            .HasForeignKey(dispatch => dispatch.RecommendationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
