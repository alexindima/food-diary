using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Services;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Ai.Services;
using FoodDiary.Application.ContentReports.Common;
using FoodDiary.Application.ContentReports.Services;
using FoodDiary.Application.DailyAdvices.Common;
using FoodDiary.Application.DailyAdvices.Services;
using FoodDiary.Application.Email.Common;
using FoodDiary.Application.Email.Services;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Application.Lessons.Services;
using FoodDiary.Application.Marketing.Common;
using FoodDiary.Application.Marketing.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application;

public static partial class DependencyInjection {
    private static void AddAdministrationModules(this IServiceCollection services) {
        services.AddScoped<IAdminImpersonationUserService, AdminImpersonationUserService>();
        services.AddScoped<IAdminAiUsageReadService, AdminAiUsageReadService>();
        services.AddScoped<IAdminAuditReadService, AdminAuditReadService>();
        services.AddScoped<IAdminBillingReadService, AdminBillingReadService>();
        services.AddScoped<IAdminContentReadService, AdminContentReadService>();
        services.AddScoped<IAdminDashboardReadService, AdminDashboardReadService>();
        services.AddScoped<IAdminUserManagementService, AdminUserManagementService>();
        services.AddScoped<IAdminUserReadService, AdminUserReadService>();
        services.AddScoped<IAdminUserLoginReadService, AdminUserLoginReadService>();
        services.AddScoped<IAiUserContextService, AiUserContextService>();
        services.AddScoped<IAiAdministrationReadService, AiAdministrationReadService>();
        services.AddScoped<IAiPromptAdministrationService, AiPromptAdministrationService>();
        services.AddScoped<IUserAiUsageSummaryReadService, UserAiUsageSummaryReadService>();
        services.AddScoped<IContentReportAdministrationService, ContentReportAdministrationService>();
        services.AddScoped<IContentReportAdministrationReadService, ContentReportAdministrationReadService>();
        services.AddScoped<IDailyAdviceReadService, DailyAdviceReadService>();
        services.AddScoped<IEmailTemplateAdministrationService, EmailTemplateAdministrationService>();
        services.AddScoped<IEmailTemplateAdministrationReadService, EmailTemplateAdministrationReadService>();
        services.AddScoped<ILessonReadService, LessonReadService>();
        services.AddScoped<ILessonAdministrationService, LessonAdministrationService>();
        services.AddScoped<ILessonAdministrationReadService, LessonAdministrationReadService>();
        services.AddScoped<IMarketingConversionRecorder, MarketingConversionRecorder>();
        services.AddScoped<IMarketingAttributionCleanupService, MarketingAttributionCleanupService>();
        services.AddScoped<IMarketingAttributionSummaryReadService, MarketingAttributionSummaryReadService>();
    }
}
