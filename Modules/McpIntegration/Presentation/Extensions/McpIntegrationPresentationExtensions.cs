using System.Security.Cryptography;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Policies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

namespace FoodDiary.Modules.McpIntegration.Presentation.Extensions;

public static class McpIntegrationPresentationExtensions {
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    extension(IServiceCollection services) {
        public IMcpServerBuilder AddMcpIntegrationPresentation(Uri issuer) {
            string metadata = new Uri(issuer, RecipePluginAccess.ResourceMetadataPath).AbsoluteUri;
            return services.AddMcpServer()
                .WithHttpTransport(transport => transport.SessionMode = HttpServerSessionMode.Stateless)
                .WithRequestFilters(filters => filters.AddCallToolFilter(next => (context, cancellationToken) =>
                    FilterCallAsync(context, next, metadata, cancellationToken)));
        }
    }

    extension(WebApplication app) {
        public WebApplication UseMcpIntegration(Uri issuer) {
            app.UseWhen(context => context.Request.Path.StartsWithSegments(RecipePluginAccess.ResourcePath, StringComparison.OrdinalIgnoreCase),
                branch => branch.Use((context, next) => AuthenticateAsync(context, next, issuer, context.RequestAborted)));
            app.MapMcp(RecipePluginAccess.ResourcePath).RequireHost(issuer.Authority).AllowAnonymous();
            return app;
        }
    }

    private static async Task AuthenticateAsync(
        HttpContext context, RequestDelegate next, Uri issuer, CancellationToken cancellationToken) {
        string origin = context.Request.Headers.Origin.ToString();
        if (origin.Length > 0 && !string.Equals(origin, issuer.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal)) {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
        AuthenticateResult result = await context.AuthenticateAsync(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        IDelegatedAccessTokenValidator validator = context.RequestServices.GetRequiredService<IDelegatedAccessTokenValidator>();
        if (!result.Succeeded || result.Principal is not { } principal ||
            !await validator.IsCurrentAsync(principal, cancellationToken)) {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer resource_metadata=\"" +
                new Uri(issuer, RecipePluginAccess.ResourceMetadataPath).AbsoluteUri + "\"";
            return;
        }
        context.User = principal;
        await next(context);
    }

    private static async ValueTask<CallToolResult> FilterCallAsync(
        RequestContext<CallToolRequestParams> context,
        McpRequestHandler<CallToolRequestParams, CallToolResult> next,
        string metadata, CancellationToken cancellationToken) {
        if (context.Params?.Name is not { } name || ScopeFor(name) is not { } scope) {
            return Error("This tool is unavailable.");
        }
        IServiceProvider services = context.Services!;
        ClaimsPrincipal user = services.GetRequiredService<IHttpContextAccessor>().HttpContext!.User;
        if (!user.HasScope(scope)) {
            CallToolResult insufficient = Error("This operation needs additional FoodDiary permission.");
            insufficient.Meta = new JsonObject {
                ["mcp/www_authenticate"] = new JsonArray(JsonValue.Create(
                    "Bearer resource_metadata=\"" + metadata + "\", error=\"insufficient_scope\", scope=\"" + scope + "\"")),
            };
            return insufficient;
        }

        Guid owner = RecipePluginAccess.GetOwner(user);
        return await HandleIdempotentWriteAsync(context, next, services.GetRequiredService<IIdempotencyStore>(),
            name, owner, cancellationToken);
    }

    private static async ValueTask<CallToolResult> HandleIdempotentWriteAsync(
        RequestContext<CallToolRequestParams> context,
        McpRequestHandler<CallToolRequestParams, CallToolResult> next,
        IIdempotencyStore store,
        string tool, Guid owner, CancellationToken cancellationToken) {
        byte[] arguments = JsonSerializer.SerializeToUtf8Bytes(context.Params!.Arguments, Json);
        string hash = Convert.ToHexString(SHA256.HashData(arguments));
        string key = $"recipe-plugin:{owner:N}:{tool}:{hash}";
        var retention = TimeSpan.FromDays(1);
        IdempotencyReservation reservation = await store.ReserveAsync(key, hash, retention,
            TimeSpan.FromMinutes(5), cancellationToken);
        if (reservation is IdempotencyReservation.Replay { Body: { } body }) {
            return JsonSerializer.Deserialize<CallToolResult>(body, Json) ??
                Error("FoodDiary could not read the result of the previous save.");
        }
        if (reservation is not IdempotencyReservation.Acquired acquired) {
            return Error("FoodDiary is already processing this save. Retry with the same details.");
        }

        CallToolResult result;
        try {
            result = await next(context, cancellationToken);
        } catch {
            await store.ReleaseAsync(key, hash, acquired.Token, CancellationToken.None);
            throw;
        }
        if (result.IsError == true) {
            await store.ReleaseAsync(key, hash, acquired.Token, CancellationToken.None);
        } else {
            await store.CompleteAsync(key, hash, acquired.Token, StatusCodes.Status201Created,
                JsonSerializer.Serialize(result, Json), location: null, retention, CancellationToken.None);
        }
        return result;
    }

    private static string? ScopeFor(string tool) => tool switch {
        RecipePluginAccess.RecipeTool => RecipePluginAccess.RecipeScope,
        RecipePluginAccess.ShoppingListTool => RecipePluginAccess.ShoppingListScope,
        _ => null,
    };

    private static CallToolResult Error(string message) => new() {
        IsError = true,
        Content = [new TextContentBlock { Text = message }],
    };
}
