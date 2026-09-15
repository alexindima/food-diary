using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.ContentReports.PersistenceModel;

public static class ContentReportsPersistenceModelRegistration {
    public static ModelBuilder ApplyContentReportsPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContentReportsPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
