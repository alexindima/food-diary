using System.Security.Cryptography.X509Certificates;
using FoodDiary.Modules.Identity.Infrastructure.Persistence;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Web.Api.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace FoodDiary.Web.Api.Extensions;

internal static class RecipePluginServiceCollectionExtensions {
    extension(IServiceCollection services) {
        public IServiceCollection AddRecipePlugin(IConfiguration configuration, IHostEnvironment? environment) {
            bool development = environment?.IsDevelopment() == true;
            if (RecipePluginOptions.ReadEnabled(configuration, development) is not { } options) {
                return services;
            }
            Uri issuer = options.Issuer!;
            Uri webOrigin = options.WebOrigin!;
            string resource = new Uri(issuer, RecipePluginAccess.ResourcePath).AbsoluteUri;
            services.AddSingleton(options);
            services.AddSingleton(new RecipePluginLinks(webOrigin));
            services.AddHostedService<RecipePluginClientRegistration>();
            services.AddAntiforgery();
            services.AddHttpContextAccessor();
            services.AddOpenIddict()
                .AddCore(core => core.UseEntityFrameworkCore().UseDbContext<IdentityDbContext>())
                .AddServer(server => {
                    server.SetIssuer(issuer)
                        .SetConfigurationEndpointUris(new Uri(issuer, "/.well-known/oauth-authorization-server"))
                        .SetJsonWebKeySetEndpointUris(new Uri(issuer, "/.well-known/jwks"))
                        .SetAuthorizationEndpointUris(new Uri(webOrigin, RecipePluginAccess.AuthorizationPath))
                        .SetTokenEndpointUris(new Uri(issuer, RecipePluginAccess.TokenPath))
                        .SetRevocationEndpointUris(new Uri(issuer, RecipePluginAccess.RevocationPath))
                        .AllowAuthorizationCodeFlow()
                        .AllowRefreshTokenFlow()
                        .RequireProofKeyForCodeExchange()
                        .RegisterScopes(RecipePluginAccess.RecipeScope, RecipePluginAccess.ShoppingListScope)
                        .RegisterResources(resource)
                        .SetAccessTokenLifetime(TimeSpan.FromMinutes(5))
                        .SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(1))
                        .SetRefreshTokenLifetime(TimeSpan.FromDays(7));
                    server.Configure(settings => {
                        settings.Scopes.Remove(Scopes.OpenId);
                        settings.CodeChallengeMethods.Remove(CodeChallengeMethods.Plain);
                        settings.ClientAuthenticationMethods.Add("none");
                    });
                    AddCertificates(server, options, development);
                    OpenIddictServerAspNetCoreBuilder aspNetCore = server.UseAspNetCore()
                        .EnableAuthorizationEndpointPassthrough()
                        .EnableTokenEndpointPassthrough();
                    if (development && (string.Equals(issuer.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal) ||
                        string.Equals(webOrigin.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal))) {
                        aspNetCore.DisableTransportSecurityRequirement();
                    }
                })
                .AddValidation(validation => {
                    validation.UseLocalServer();
                    validation.AddAudiences(resource);
                    validation.EnableTokenEntryValidation();
                    validation.EnableAuthorizationEntryValidation();
                    validation.Configure(settings => settings.TokenValidationParameters.ClockSkew = TimeSpan.Zero);
                    validation.UseAspNetCore();
                });
            return services.AddRecipePluginMcp(options);
        }
    }

    private static void AddCertificates(OpenIddictServerBuilder builder, RecipePluginOptions options, bool development) {
        if (development && options.SigningCertificatePath is null && options.EncryptionCertificatePath is null) {
            builder.AddEphemeralSigningKey().AddEphemeralEncryptionKey();
            return;
        }

        builder.AddSigningCertificate(X509CertificateLoader.LoadPkcs12FromFile(
            options.SigningCertificatePath ?? throw new InvalidOperationException("Missing OAuth signing certificate."),
            options.SigningCertificatePassword, X509KeyStorageFlags.EphemeralKeySet));
        builder.AddEncryptionCertificate(X509CertificateLoader.LoadPkcs12FromFile(
            options.EncryptionCertificatePath ?? throw new InvalidOperationException("Missing OAuth encryption certificate."),
            options.EncryptionCertificatePassword, X509KeyStorageFlags.EphemeralKeySet));
    }

    private sealed class RecipePluginClientRegistration(IServiceScopeFactory scopes, RecipePluginOptions options) : IHostedService {
        public async Task StartAsync(CancellationToken cancellationToken) {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            IOpenIddictApplicationManager applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
            await RegisterAsync(applications, options.ClientId, options.RedirectUri!, "FoodDiary for ChatGPT", cancellationToken);
            if (options.CodexClientId is { } codexClientId && options.CodexRedirectUri is { } codexRedirectUri) {
                await RegisterAsync(applications, codexClientId, codexRedirectUri, "FoodDiary for Codex", cancellationToken);
            }
        }

        private async Task RegisterAsync(IOpenIddictApplicationManager applications, string clientId, Uri redirectUri,
            string displayName, CancellationToken cancellationToken) {
            var client = new OpenIddictApplicationDescriptor {
                ClientId = clientId,
                ClientType = ClientTypes.Public,
                DisplayName = displayName,
                RedirectUris = { redirectUri },
                Permissions = {
                    Permissions.Endpoints.Authorization, Permissions.Endpoints.Token, Permissions.Endpoints.Revocation,
                    Permissions.GrantTypes.AuthorizationCode, Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Prefixes.Scope + RecipePluginAccess.RecipeScope,
                    Permissions.Prefixes.Scope + RecipePluginAccess.ShoppingListScope,
                    Permissions.Prefixes.Resource + new Uri(options.Issuer!, RecipePluginAccess.ResourcePath).AbsoluteUri,
                },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange },
            };
            object? existing = await applications.FindByClientIdAsync(clientId, cancellationToken);
            if (existing is null) {
                await applications.CreateAsync(client, cancellationToken);
            } else if (!await applications.ValidateRedirectUriAsync(existing, redirectUri.AbsoluteUri, cancellationToken)) {
                throw new InvalidOperationException("RecipePlugin callback differs from the registered OAuth client.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
