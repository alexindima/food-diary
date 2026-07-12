using FoodDiary.Application;
using FoodDiary.Application.Marketing;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Integrations;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Infrastructure;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Resources.Notifications;
using FoodDiary.Resources.Reports;
using FoodDiary.Web.Api.Services;
using StackExchange.Redis;

namespace FoodDiary.Web.Api.Extensions;

public static class ApiServiceCollectionExtensions {
    extension(IServiceCollection services) {
        public IServiceCollection AddApiServices(IConfiguration configuration, IHostEnvironment? environment = null) {
            services.AddApplicationModules(configuration, environment);
            services.AddApiOptions();
            services.AddApiAuthentication();
            services.AddApiHostServices();
            services.AddApiDataProtection(configuration);
            services.AddApiSwagger();
            services.AddConfiguredOpenTelemetry();
            services.AddApiHealthChecks();

            return services;
        }
        private IServiceCollection AddApplicationModules(IConfiguration configuration, IHostEnvironment? environment) {
            services.AddApplication();
            services.AddMarketingModule();
            services.AddInfrastructure(configuration);
            services.AddIntegrations(configuration);
            services.AddSingleton<INotificationTextRenderer, NotificationResourceRenderer>();
            services.AddSingleton<IDiaryPdfReportTextProvider, DiaryPdfReportResourceTextProvider>();
            services.AddApiDistributedCache(configuration, environment);
            services.AddPresentationApi();
            services.AddEndpointsApiExplorer();

            return services;
        }
        private IServiceCollection AddApiDistributedCache(IConfiguration configuration, IHostEnvironment? environment) {
            string? redisConnectionString = configuration.GetConnectionString("Redis");
            if (string.IsNullOrWhiteSpace(redisConnectionString)) {
                if (environment?.IsDevelopment() == false) {
                    throw new InvalidOperationException("ConnectionStrings:Redis is required outside Development.");
                }

                services.AddDistributedMemoryCache();
                return services;
            }

            var redisConnection = new Lazy<IConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(redisConnectionString));
            services.AddSingleton(_ => redisConnection.Value);
            services.AddStackExchangeRedisCache(options => {
                options.Configuration = redisConnectionString;
                options.InstanceName = "fooddiary:";
                options.ConnectionMultiplexerFactory = () => Task.FromResult(redisConnection.Value);
            });
            services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();

            return services;
        }
    }
}
