using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Dietologist.Infrastructure.Persistence;

public sealed class DietologistDbContext(DbContextOptions<DietologistDbContext> options) : DbContext(options) {
    public DbSet<DietologistInvitation> DietologistInvitations => Set<DietologistInvitation>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<RecommendationComment> RecommendationComments => Set<RecommendationComment>();
    public DbSet<RecommendationTemplate> RecommendationTemplates => Set<RecommendationTemplate>();
    public DbSet<RecommendationBulkDispatch> RecommendationBulkDispatches => Set<RecommendationBulkDispatch>();
    public DbSet<ClientTask> ClientTasks => Set<ClientTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyDietologistPersistenceModel();
}
