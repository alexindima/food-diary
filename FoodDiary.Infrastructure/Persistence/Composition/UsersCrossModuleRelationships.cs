using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Composition;

internal static class UsersCrossModuleRelationships {
    internal static void Configure(ModelBuilder modelBuilder) {
        modelBuilder.Entity<User>().HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(user => user.ProfileImageAssetId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.ClientNoAction);
    }
}
