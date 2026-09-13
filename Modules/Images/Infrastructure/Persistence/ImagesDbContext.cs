using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Images;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Images.Infrastructure.Persistence;

public sealed class ImagesDbContext(DbContextOptions<ImagesDbContext> options) : DbContext(options) {
    public DbSet<ImageAsset> ImageAssets => Set<ImageAsset>();
    public DbSet<ImageObjectDeletionOutboxMessage> ImageObjectDeletionOutbox => Set<ImageObjectDeletionOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyImagesPersistenceModel();
    }
}
