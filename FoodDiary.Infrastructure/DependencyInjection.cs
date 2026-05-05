using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Abstractions.DailyAdvices.Common;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Application.Abstractions.RecipeLikes.Common;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Infrastructure.Authentication;
using FoodDiary.Infrastructure.Persistence.Admin;
using FoodDiary.Infrastructure.Persistence.Ai;
using FoodDiary.Infrastructure.Persistence.Billing;
using FoodDiary.Infrastructure.Persistence.Images;
using FoodDiary.Infrastructure.Persistence.Meals;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Interceptors;
using FoodDiary.Infrastructure.Persistence.Products;
using FoodDiary.Infrastructure.Persistence.RecentItems;
using FoodDiary.Infrastructure.Persistence.Recipes;
using FoodDiary.Infrastructure.Persistence.Dietologist;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Infrastructure.Persistence.Notifications;
using FoodDiary.Infrastructure.Persistence.OpenFoodFacts;
using FoodDiary.Infrastructure.Persistence.Recommendations;
using FoodDiary.Infrastructure.Persistence.Content;
using FoodDiary.Infrastructure.Persistence.ShoppingLists;
using FoodDiary.Infrastructure.Persistence.MealPlans;
using FoodDiary.Infrastructure.Persistence.FavoriteMeals;
using FoodDiary.Infrastructure.Persistence.FavoriteProducts;
using FoodDiary.Infrastructure.Persistence.FavoriteRecipes;
using FoodDiary.Infrastructure.Persistence.ContentReports;
using FoodDiary.Infrastructure.Persistence.RecipeComments;
using FoodDiary.Infrastructure.Persistence.RecipeLikes;
using FoodDiary.Infrastructure.Persistence.Usda;
using FoodDiary.Infrastructure.Persistence.Wearables;
using FoodDiary.Infrastructure.Persistence.Tracking;
using FoodDiary.Infrastructure.Persistence.Users;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Events;
using FoodDiary.Infrastructure.Services;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure;

public static class DependencyInjection {
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) {
        services.AddMemoryCache();
        services.AddLogging();
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(static options => !options.EnableRetries || options.MaxRetryCount > 0,
                "Database:MaxRetryCount must be greater than zero when retries are enabled.")
            .Validate(static options => !options.EnableRetries || options.MaxRetryDelaySeconds > 0,
                "Database:MaxRetryDelaySeconds must be greater than zero when retries are enabled.")
            .ValidateOnStart();
        services.AddSingleton<DatabaseCommandTelemetryInterceptor>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IDomainEventPublisher, MediatorDomainEventPublisher>();
        services.AddScoped<DomainEventDispatchInterceptor>();
        services.AddDbContext<FoodDiaryDbContext>((sp, options) => {
            var databaseOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options
                .UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => {
                        if (databaseOptions.EnableRetries) {
                            npgsqlOptions.EnableRetryOnFailure(
                                databaseOptions.MaxRetryCount,
                                TimeSpan.FromSeconds(databaseOptions.MaxRetryDelaySeconds),
                                errorCodesToAdd: null);
                        }
                    })
                .AddInterceptors(
                    sp.GetRequiredService<DatabaseCommandTelemetryInterceptor>(),
                    sp.GetRequiredService<DomainEventDispatchInterceptor>());
        });

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(static options => !string.IsNullOrWhiteSpace(options.SecretKey) && options.SecretKey.Length >= 32,
                $"{JwtOptions.SectionName}:SecretKey must be at least 32 characters long.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.Issuer),
                $"{JwtOptions.SectionName}:Issuer is required.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.Audience),
                $"{JwtOptions.SectionName}:Audience is required.")
            .Validate(static options => options.ExpirationMinutes > 0,
                $"{JwtOptions.SectionName}:ExpirationMinutes must be greater than zero.")
            .Validate(static options => options.RefreshTokenExpirationDays > 0,
                $"{JwtOptions.SectionName}:RefreshTokenExpirationDays must be greater than zero.")
            .ValidateOnStart();
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .Validate(EmailOptions.HasValidFrontendBaseUrl,
                "Email:FrontendBaseUrl must be an absolute HTTP(S) URL when provided.")
            .Validate(EmailOptions.HasValidAllowedFrontendBaseUrls,
                "Email:AllowedFrontendBaseUrls entries must be absolute HTTP(S) URLs.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.VerificationPath),
                "Email:VerificationPath is required.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.PasswordResetPath),
                "Email:PasswordResetPath is required.")
            .ValidateOnStart();
        services.AddSingleton(static sp => sp.GetRequiredService<IOptions<EmailOptions>>().Value);

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserLoginEventRepository, UserLoginEventRepository>();
        services.AddScoped<IBillingSubscriptionRepository, BillingSubscriptionRepository>();
        services.AddScoped<IBillingPaymentRepository, BillingPaymentRepository>();
        services.AddScoped<IBillingWebhookEventRepository, BillingWebhookEventRepository>();
        services.AddScoped<IAdminBillingRepository, AdminBillingRepository>();
        services.AddScoped<IAdminImpersonationSessionRepository, AdminImpersonationSessionRepository>();
        services.AddScoped<ProductRepository>();
        services.AddScoped<IProductRepository, CachedProductRepository>();
        services.AddScoped<IProductLookupService, ProductLookupService>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IRecipeLookupService, RecipeLookupService>();
        services.AddScoped<IRecentItemRepository, RecentItemRepository>();
        services.AddScoped<IMealRepository, MealRepository>();
        services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
        services.AddScoped<IWeightEntryRepository, WeightEntryRepository>();
        services.AddScoped<IWaistEntryRepository, WaistEntryRepository>();
        services.AddScoped<IHydrationEntryRepository, HydrationEntryRepository>();
        services.AddScoped<IDailyAdviceRepository, DailyAdviceRepository>();
        services.AddScoped<ICycleRepository, CycleRepository>();
        services.AddScoped<IImageAssetRepository, ImageAssetRepository>();
        services.AddScoped<IAiUsageRepository, AiUsageRepository>();
        services.AddScoped<IAiPromptTemplateRepository, AiPromptTemplateRepository>();
        services.AddSingleton<IAiPromptProvider, AiPromptProvider>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IDietologistInvitationRepository, DietologistInvitationRepository>();
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IWebPushSubscriptionRepository, WebPushSubscriptionRepository>();
        services.AddScoped<IOpenFoodFactsProductCacheRepository, OpenFoodFactsProductCacheRepository>();
        services.AddScoped<IFastingPlanRepository, FastingPlanRepository>();
        services.AddScoped<IFastingOccurrenceRepository, FastingOccurrenceRepository>();
        services.AddScoped<IFastingCheckInRepository, FastingCheckInRepository>();
        services.AddScoped<IFastingSessionRepository, FastingSessionRepository>();
        services.AddScoped<IFastingTelemetryEventRepository, FastingTelemetryEventRepository>();
        services.AddScoped<IFavoriteMealRepository, FavoriteMealRepository>();
        services.AddScoped<IFavoriteProductRepository, FavoriteProductRepository>();
        services.AddScoped<IFavoriteRecipeRepository, FavoriteRecipeRepository>();
        services.AddScoped<IExerciseEntryRepository, ExerciseEntryRepository>();
        services.AddScoped<INutritionLessonRepository, NutritionLessonRepository>();
        services.AddScoped<IMealPlanRepository, MealPlanRepository>();
        services.AddScoped<IRecipeCommentRepository, RecipeCommentRepository>();
        services.AddScoped<IRecipeLikeRepository, RecipeLikeRepository>();
        services.AddScoped<IContentReportRepository, ContentReportRepository>();
        services.AddScoped<IUsdaFoodRepository, UsdaFoodRepository>();
        services.AddScoped<IWearableConnectionRepository, WearableConnectionRepository>();
        services.AddScoped<IWearableSyncRepository, WearableSyncRepository>();
        services.AddHttpClient<IDiaryPdfGenerator, DiaryPdfGenerator>(client => {
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IAdminSsoService, AdminSsoService>();
        services.AddScoped<IUserCleanupService, UserCleanupService>();
        services.AddSingleton<IEmailTemplateProvider, EmailTemplateProvider>();
        services.AddSingleton<IAuditLogger, StructuredAuditLogger>();

        return services;
    }
}
