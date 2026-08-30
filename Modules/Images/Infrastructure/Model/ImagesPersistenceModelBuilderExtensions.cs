using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public static class ImagesPersistenceModelBuilderExtensions {
    public static ModelBuilder ApplyImagesPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ImagesPersistenceModelBuilderExtensions).Assembly);
        return modelBuilder;
    }
}
