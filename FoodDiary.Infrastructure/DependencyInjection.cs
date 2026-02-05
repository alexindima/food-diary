using Amazon.S3;
using Amazon;
using Amazon.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using FoodDiary.Application.Common.Interfaces.Authentication;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Infrastructure.Authentication;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Services;
using FoodDiary.Application.Common.Interfaces.Services;

namespace FoodDiary.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FoodDiaryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));
        services.Configure<TelegramAuthOptions>(configuration.GetSection(TelegramAuthOptions.SectionName));
        services.Configure<TelegramBotOptions>(configuration.GetSection(TelegramBotOptions.SectionName));
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IMealRepository, MealRepository>();
        services.AddScoped<IWeightEntryRepository, WeightEntryRepository>();
        services.AddScoped<IWaistEntryRepository, WaistEntryRepository>();
        services.AddScoped<IHydrationEntryRepository, HydrationEntryRepository>();
        services.AddScoped<IDailyAdviceRepository, DailyAdviceRepository>();
        services.AddScoped<ICycleRepository, CycleRepository>();
        services.AddScoped<IImageAssetRepository, ImageAssetRepository>();
        services.AddSingleton<IAmazonS3>(provider =>
        {
            var s3Options = provider.GetRequiredService<IOptions<S3Options>>().Value;
            var credentials = new BasicAWSCredentials(s3Options.AccessKeyId, s3Options.SecretAccessKey);
            var regionValue = s3Options.Region?.Trim();
            RegionEndpoint? regionEndpoint = null;
            if (!string.IsNullOrWhiteSpace(regionValue))
            {
                regionEndpoint = RegionEndpoint.GetBySystemName(regionValue);
            }
            regionEndpoint ??= RegionEndpoint.USEast1;

            var config = new AmazonS3Config
            {
                RegionEndpoint = regionEndpoint,
                AuthenticationRegion = regionEndpoint.SystemName,
                ServiceURL = s3Options.ServiceUrl,
                ForcePathStyle = !string.IsNullOrWhiteSpace(s3Options.ServiceUrl)
            };

            return new AmazonS3Client(credentials, config);
        });
        services.AddSingleton<IImageStorageService, S3ImageStorageService>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITelegramAuthValidator, TelegramAuthValidator>();
        services.AddSingleton<ITelegramLoginWidgetValidator, TelegramLoginWidgetValidator>();
        services.AddSingleton<IAdminSsoService, AdminSsoService>();
        services.AddScoped<IUserCleanupService, UserCleanupService>();
        services.AddHttpClient<IOpenAiFoodService, OpenAiFoodService>();

        return services;
    }
}
