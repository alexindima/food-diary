using FoodDiary.Application;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Integrations;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Infrastructure;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Resources.Notifications;
using FoodDiary.Resources.Reports;

namespace FoodDiary.Web.Api.Extensions;

public static class ApiServiceCollectionExtensions {
    extension(IServiceCollection services) {
        public IServiceCollection AddApiServices(IConfiguration configuration) {
            services.AddApplicationModules(configuration);
            services.AddApiOptions();
            services.AddApiAuthentication();
            services.AddApiHostServices();
            services.AddApiDataProtection(configuration);
            services.AddApiSwagger();
            services.AddConfiguredOpenTelemetry();
            services.AddApiHealthChecks();

            return services;
        }
        private IServiceCollection AddApplicationModules(IConfiguration configuration) {
            services.AddApplication();
            services.AddInfrastructure(configuration);
            services.AddIntegrations(configuration);
            services.AddSingleton<INotificationTextRenderer, NotificationResourceRenderer>();
            services.AddSingleton<IDiaryPdfReportTextProvider, DiaryPdfReportResourceTextProvider>();
            services.AddApiDistributedCache(configuration);
            services.AddPresentationApi();
            services.AddEndpointsApiExplorer();

            return services;
        }
        private IServiceCollection AddApiDistributedCache(IConfiguration configuration) {
            string? redisConnectionString = configuration.GetConnectionString("Redis");
            if (string.IsNullOrWhiteSpace(redisConnectionString)) {
                services.AddDistributedMemoryCache();
                return services;
            }

            services.AddStackExchangeRedisCache(options => {
                options.Configuration = redisConnectionString;
                options.InstanceName = "fooddiary:";
            });

            return services;
        }
    }
}
