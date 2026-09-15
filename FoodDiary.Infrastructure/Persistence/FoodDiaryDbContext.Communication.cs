using FoodDiary.Modules.ContentReports.Domain.Entities;
using FoodDiary.Modules.Admin.PersistenceModel;
using FoodDiary.Modules.Identity.Domain.Entities.Content;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Modules.Notifications.PersistenceModel;
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
    public DbSet<BugAcknowledgementReceipt> BugAcknowledgementReceipts => Set<BugAcknowledgementReceipt>();
    public DbSet<NotificationWebPushOutboxMessage> NotificationWebPushOutbox => Set<NotificationWebPushOutboxMessage>();
    public DbSet<WebPushSubscription> WebPushSubscriptions => Set<WebPushSubscription>();
    public DbSet<ContentReport> ContentReports => Set<ContentReport>();
}
