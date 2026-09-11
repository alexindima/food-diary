using FoodDiary.Infrastructure.Persistence.Configurations.Ai;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public static class AiPersistenceModelRegistration {
    public static ModelBuilder ApplyAiPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfiguration(new AiUsageConfiguration());
        modelBuilder.ApplyConfiguration(new AiPromptTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new AiQuotaPeriodConfiguration());
        modelBuilder.ApplyConfiguration(new AiQuotaReservationConfiguration());
        modelBuilder.ApplyConfiguration(new FoodRecognitionJobConfiguration());
        return modelBuilder;
    }
}
