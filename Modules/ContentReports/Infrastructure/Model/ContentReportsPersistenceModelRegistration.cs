using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.ContentReports.Infrastructure.Persistence;

public static class ContentReportsPersistenceModelRegistration {
    public static ModelBuilder ApplyContentReportsPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContentReportsPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
