using FoodDiary.Modules.Images.PersistenceModel;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Images.PersistenceModel.Images;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Images.Infrastructure.Persistence;

public sealed class ImagesDbContext(DbContextOptions<ImagesDbContext> options) : DbContext(options) {
    public DbSet<ImageAsset> ImageAssets => Set<ImageAsset>();
    public DbSet<ImageObjectDeletionOutboxMessage> ImageObjectDeletionOutbox => Set<ImageObjectDeletionOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyImagesPersistenceModel();
    }
}
