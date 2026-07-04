using FoodDiary.Domain.Entities.Wearables;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public sealed partial class FoodDiaryDbContext {
    public DbSet<WearableConnection> WearableConnections => Set<WearableConnection>();
    public DbSet<WearableSyncEntry> WearableSyncEntries => Set<WearableSyncEntry>();
}
