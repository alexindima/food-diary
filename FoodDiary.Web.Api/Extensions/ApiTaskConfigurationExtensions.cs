using Microsoft.Extensions.Http;

namespace FoodDiary.Web.Api.Extensions;

public static class ApiTaskConfigurationExtensions {
    extension(IConfigurationBuilder configuration) {
        /// <summary>Task runtimes load one owned configuration and never inherit user secrets or provider credentials.</summary>
        public IConfigurationBuilder AddTaskConfiguration(IHostEnvironment environment, string? configurationPath) {
            if (string.IsNullOrWhiteSpace(configurationPath)) {
                return configuration;
            }
            if (!environment.IsDevelopment()) {
                throw new InvalidOperationException("FOODDIARY_TASK_CONFIG is allowed only in Development.");
            }
            if (!Path.IsPathFullyQualified(configurationPath) || !File.Exists(configurationPath)) {
                throw new InvalidOperationException("Task configuration must be an existing absolute file owned by the local task runtime.");
            }

            configuration.Sources.Clear();
            return configuration.AddJsonFile(configurationPath, optional: false, reloadOnChange: false);
        }
    }

    extension(IServiceCollection services) {
        public IServiceCollection AddTaskOutboundGuard() {
            services.AddSingleton<IHttpMessageHandlerBuilderFilter, TaskOutboundGuardFilter>();
            return services;
        }
    }

    private sealed class TaskOutboundGuardFilter : IHttpMessageHandlerBuilderFilter {
        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next) {
            return builder => {
                next(builder);
                if (builder.PrimaryHandler is SocketsHttpHandler sockets) {
                    sockets.AllowAutoRedirect = false;
                } else if (builder.PrimaryHandler is HttpClientHandler client) {
                    client.AllowAutoRedirect = false;
                } else {
                    throw new InvalidOperationException("Task provider transport must support disabling redirects.");
                }
                builder.AdditionalHandlers.Insert(0, new TaskOutboundHttpHandler());
            };
        }
    }
}
