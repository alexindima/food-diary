using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Images.PersistenceModel;

public static class ImagesPersistenceModelBuilderExtensions {
    public static ModelBuilder ApplyImagesPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ImagesPersistenceModelBuilderExtensions).Assembly);
        return modelBuilder;
    }
}
