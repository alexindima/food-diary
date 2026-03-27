using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Application.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Infrastructure.Authentication;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Services;

namespace FoodDiary.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddDbContext<FoodDiaryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddOptions<S3Options>()
            .Bind(configuration.GetSection(S3Options.SectionName))
            .Validate(static options => options.MaxUploadSizeBytes > 0,
                "S3:MaxUploadSizeBytes must be greater than zero.")
            .Validate(static options => string.IsNullOrWhiteSpace(options.PublicBaseUrl) || Uri.IsWellFormedUriString(options.PublicBaseUrl, UriKind.Absolute),
                "S3:PublicBaseUrl must be an absolute URL when provided.")
            .Validate(static options => string.IsNullOrWhiteSpace(options.ServiceUrl) || Uri.IsWellFormedUriString(options.ServiceUrl, UriKind.Absolute),
                "S3:ServiceUrl must be an absolute URL when provided.")
            .ValidateOnStart();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(static options => !string.IsNullOrWhiteSpace(options.SecretKey) && options.SecretKey.Length >= 32,
                "JwtSettings:SecretKey must be at least 32 characters long.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.Issuer),
                "JwtSettings:Issuer is required.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.Audience),
                "JwtSettings:Audience is required.")
            .Validate(static options => options.ExpirationMinutes > 0,
                "JwtSettings:ExpirationMinutes must be greater than zero.")
            .Validate(static options => options.RefreshTokenExpirationDays > 0,
                "JwtSettings:RefreshTokenExpirationDays must be greater than zero.")
            .ValidateOnStart();
        services.AddOptions<TelegramAuthOptions>()
            .Bind(configuration.GetSection(TelegramAuthOptions.SectionName));
        services.AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection(OpenAiOptions.SectionName))
            .Validate(static options => string.IsNullOrWhiteSpace(options.VisionModel) || !string.IsNullOrWhiteSpace(options.VisionFallbackModel),
                "OpenAi:VisionFallbackModel is required when VisionModel is configured.")
            .Validate(static options => string.IsNullOrWhiteSpace(options.ApiKey) || !string.IsNullOrWhiteSpace(options.TextModel),
                "OpenAi:TextModel is required when ApiKey is configured.")
            .Validate(static options => string.IsNullOrWhiteSpace(options.ApiKey) || !string.IsNullOrWhiteSpace(options.VisionModel),
                "OpenAi:VisionModel is required when ApiKey is configured.")
            .ValidateOnStart();
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .Validate(static options => options.SmtpPort > 0,
                "Email:SmtpPort must be greater than zero.")
            .Validate(static options => string.IsNullOrWhiteSpace(options.FrontendBaseUrl) || Uri.IsWellFormedUriString(options.FrontendBaseUrl, UriKind.Absolute),
                "Email:FrontendBaseUrl must be an absolute URL when provided.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.VerificationPath),
                "Email:VerificationPath is required.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.PasswordResetPath),
                "Email:PasswordResetPath is required.")
            .ValidateOnStart();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
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
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddSingleton<IImageStorageService, S3ImageStorageService>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITelegramAuthValidator, TelegramAuthValidator>();
        services.AddSingleton<ITelegramLoginWidgetValidator, TelegramLoginWidgetValidator>();
        services.AddSingleton<IAdminSsoService, AdminSsoService>();
        services.AddScoped<IUserCleanupService, UserCleanupService>();
        services.AddSingleton<IEmailTemplateProvider, EmailTemplateProvider>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddHttpClient<IOpenAiFoodService, OpenAiFoodService>(client => {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }
}
