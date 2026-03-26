using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Application.Authentication.Abstractions;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
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

        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));
        services.Configure<TelegramAuthOptions>(configuration.GetSection(TelegramAuthOptions.SectionName));
        services.Configure<TelegramBotOptions>(configuration.GetSection(TelegramBotOptions.SectionName));
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

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
        services.AddHttpClient<IOpenAiFoodService, OpenAiFoodService>();

        return services;
    }
}
