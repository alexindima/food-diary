using FoodDiary.Modules.Ai.Domain.Entities;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Ai.PersistenceModel;
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
        modelBuilder.Entity<FoodRecognitionJobImage>().HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(x => x.ImageAssetId)
            .OnDelete(DeleteBehavior.ClientNoAction);
        modelBuilder.Entity<FoodRecognitionJob>().HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(x => x.ImageAssetId)
            .OnDelete(DeleteBehavior.ClientNoAction);
    }
}
