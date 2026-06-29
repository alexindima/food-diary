using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Social;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public partial class FoodDiaryDbContext {
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<DietologistInvitation> DietologistInvitations => Set<DietologistInvitation>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<WebPushSubscription> WebPushSubscriptions => Set<WebPushSubscription>();
    public DbSet<ContentReport> ContentReports => Set<ContentReport>();
}
