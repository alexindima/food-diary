using FoodDiary.Presentation.Api.Options;
using FoodDiary.Web.Api.Options;

namespace FoodDiary.Web.Api.Extensions;

public static class ApiOptionsServiceCollectionExtensions {
    extension(IServiceCollection services) {
        internal IServiceCollection AddApiOptions() {
            services.AddHostBoundaryOptions();
            services.AddTelemetryAndAuthOptions();

            return services;
        }

        private IServiceCollection AddHostBoundaryOptions() {
            services
                .AddOptions<ApiDataProtectionOptions>()
                .BindConfiguration(ApiDataProtectionOptions.SectionName)
                .Validate(ApiDataProtectionOptions.HasValidApplicationName,
                    "DataProtection:ApplicationName must not be empty.")
                .ValidateOnStart();
            services
                .AddOptions<ApiCorsOptions>()
                .BindConfiguration(ApiCorsOptions.SectionName)
                .Validate(ApiCorsOptions.HasValidOrigins,
                    "Cors:Origins must contain at least one absolute origin URL.")
                .ValidateOnStart();
            services
                .AddOptions<ApiForwardedHeadersOptions>()
                .BindConfiguration(ApiForwardedHeadersOptions.SectionName)
                .Validate(ApiForwardedHeadersOptions.HasValidForwardLimit,
                    "ForwardedHeaders:ForwardLimit must be greater than zero.")
                .Validate(ApiForwardedHeadersOptions.HasValidKnownProxies,
                    "ForwardedHeaders:KnownProxies must contain valid IP addresses.")
                .Validate(ApiForwardedHeadersOptions.HasValidKnownNetworks,
                    "ForwardedHeaders:KnownNetworks must contain valid CIDR entries.")
                .ValidateOnStart();
            services
                .AddOptions<ApiHttpsRedirectionOptions>()
                .BindConfiguration(ApiHttpsRedirectionOptions.SectionName)
                .ValidateOnStart();
            services
                .AddOptions<ApiRateLimitingOptions>()
                .BindConfiguration(ApiRateLimitingOptions.SectionName)
                .Validate(ApiRateLimitingOptions.HasValidAuth,
                    "RateLimiting:Auth requires positive PermitLimit/WindowSeconds and non-negative QueueLimit.")
                .Validate(ApiRateLimitingOptions.HasValidAi,
                    "RateLimiting:Ai requires positive PermitLimit/WindowSeconds and non-negative QueueLimit.")
                .ValidateOnStart();
            services
                .AddOptions<ApiOutputCacheOptions>()
                .BindConfiguration(ApiOutputCacheOptions.SectionName)
                .Validate(ApiOutputCacheOptions.HasValidAdminAiUsage,
                    "OutputCache:AdminAiUsage:ExpirationSeconds must be greater than zero.")
                .Validate(ApiOutputCacheOptions.HasValidUserScoped,
                    "OutputCache:UserScoped:ExpirationSeconds must be greater than zero.")
                .ValidateOnStart();

            return services;
        }

        private IServiceCollection AddTelemetryAndAuthOptions() {
            services
                .AddOptions<OpenTelemetryOptions>()
                .BindConfiguration(OpenTelemetryOptions.SectionName)
                .Validate(OpenTelemetryOptions.HasValidOtlpEndpoint,
                    "OpenTelemetry:Otlp:Endpoint must be a valid absolute URI when provided.")
                .ValidateOnStart();
            services
                .AddOptions<TelegramBotAuthOptions>()
                .BindConfiguration(TelegramBotAuthOptions.SectionName)
                .Validate(TelegramBotAuthOptions.HasValidApiSecret,
                    "TelegramBot:ApiSecret must be empty or at least 16 characters long.")
                .ValidateOnStart();

            services
                .AddOptions<ApiBuildInfoOptions>()
                .BindConfiguration(ApiBuildInfoOptions.SectionName)
                .Validate(ApiBuildInfoOptions.HasValidCommitSha,
                    "BuildInfo:CommitSha must be empty or shorter than 129 characters.")
                .Validate(ApiBuildInfoOptions.HasValidImageTag,
                    "BuildInfo:ImageTag must be empty or shorter than 257 characters.")
                .ValidateOnStart();

            return services;
        }
    }
}
