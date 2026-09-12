using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Composition;

internal static class DietologistCrossModuleRelationships {
    internal static void Configure(ModelBuilder modelBuilder) {
        modelBuilder.Entity<ClientTask>().HasOne<User>()
            .WithMany()
            .HasForeignKey(task => task.DietologistUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClientTask>().HasOne<User>()
            .WithMany()
            .HasForeignKey(task => task.ClientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DietologistInvitation>().HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.ClientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DietologistInvitation>().HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.DietologistUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<RecommendationBulkDispatch>().HasOne<User>()
            .WithMany()
            .HasForeignKey(dispatch => dispatch.DietologistUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RecommendationBulkDispatch>().HasOne<User>()
            .WithMany()
            .HasForeignKey(dispatch => dispatch.ClientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RecommendationComment>().HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.AuthorUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Recommendation>().HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.DietologistUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Recommendation>().HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.ClientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RecommendationTemplate>().HasOne<User>()
            .WithMany()
            .HasForeignKey(template => template.DietologistUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
