using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Infrastructure.Persistence.Notifications;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public sealed partial class FoodDiaryDbContext {
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<DietologistInvitation> DietologistInvitations => Set<DietologistInvitation>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<RecommendationComment> RecommendationComments => Set<RecommendationComment>();
    public DbSet<ClientTask> ClientTasks => Set<ClientTask>();
    public DbSet<RecommendationTemplate> RecommendationTemplates => Set<RecommendationTemplate>();
    public DbSet<RecommendationBulkDispatch> RecommendationBulkDispatches => Set<RecommendationBulkDispatch>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<EmailOutboxMessage> EmailOutbox => Set<EmailOutboxMessage>();
    public DbSet<NotificationWebPushOutboxMessage> NotificationWebPushOutbox => Set<NotificationWebPushOutboxMessage>();
    public DbSet<WebPushSubscription> WebPushSubscriptions => Set<WebPushSubscription>();
    public DbSet<ContentReport> ContentReports => Set<ContentReport>();
}
