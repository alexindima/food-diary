using Asp.Versioning;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class PresentationServiceCollectionExtensions {
    extension(IServiceCollection services) {
        public IServiceCollection AddPresentationApi() {
            services.AddScoped<TelemetryActionFilter>();
            services.AddScoped<IdempotencyFilter>();
            services.TryAddSingleton(TimeProvider.System);
            services.TryAddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
            services.AddApiVersioning(options => {
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            }).AddMvc();
            services
                .AddControllers(options => {
                    options.Filters.AddService<TelemetryActionFilter>();
                    options.Filters.AddService<IdempotencyFilter>();
                })
                .ConfigureApiBehaviorOptions(options => {
                    options.InvalidModelStateResponseFactory = context => {
                        var errors = context.ModelState
                            .Where(static entry => entry.Value!.Errors.Count > 0)
                            .ToDictionary(
                                static entry => ApiErrorDetailsMapper.ToCamelCasePath(string.IsNullOrWhiteSpace(entry.Key) ? "request" : entry.Key),
                                static entry => entry.Value!.Errors
                                    .Select(static error => error.ErrorMessage)
                                    .Where(static message => !string.IsNullOrWhiteSpace(message))
                                    .DefaultIfEmpty("The value is invalid.")
                                    .ToArray(),
                                StringComparer.Ordinal);

                        return new BadRequestObjectResult(new ApiErrorHttpResponse(
                            "Validation.Invalid",
                            "One or more validation errors occurred.",
                            context.HttpContext.TraceIdentifier,
                            errors.Count > 0 ? errors : null));
                    };
                });
            services.AddSignalR();
            services.AddSingleton<IUserIdProvider, UserIdProvider>();
            return services;
        }

        public IServiceCollection AddPresentationAssembly(Assembly assembly) {
            services.AddControllers().AddApplicationPart(assembly);
            return services;
        }
    }
}
