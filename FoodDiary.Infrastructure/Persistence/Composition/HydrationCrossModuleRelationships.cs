using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Composition;

internal static class HydrationCrossModuleRelationships {
    internal static void Configure(ModelBuilder modelBuilder) {
        modelBuilder.Entity<HydrationEntry>().HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<HydrationOperationReceipt>().HasOne<User>()
            .WithMany()
            .HasForeignKey(receipt => receipt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
