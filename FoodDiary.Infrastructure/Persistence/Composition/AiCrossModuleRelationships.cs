using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence.Ai;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Composition;

internal static class AiCrossModuleRelationships {
    internal static void Configure(ModelBuilder modelBuilder) {
        modelBuilder.Entity<AiUsage>().HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AiQuotaPeriod>().HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<FoodRecognitionJob>().HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<FoodRecognitionJob>().HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(x => x.ImageAssetId)
            .OnDelete(DeleteBehavior.ClientNoAction);
    }
}
