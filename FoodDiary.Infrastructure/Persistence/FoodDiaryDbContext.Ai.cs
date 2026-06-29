using FoodDiary.Domain.Entities.Ai;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public partial class FoodDiaryDbContext {
    public DbSet<AiUsage> AiUsages => Set<AiUsage>();
    public DbSet<AiPromptTemplate> AiPromptTemplates => Set<AiPromptTemplate>();
}
