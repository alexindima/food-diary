using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Integrations.Options;
using FoodDiary.Results;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations.Authentication;

public sealed class TelegramOidcProvider(HttpClient httpClient, IOptions<TelegramOidcOptions> options, ITelegramOidcTokenValidator tokens)
    : ITelegramOidcProvider {
    public bool IsEnabled => options.Value.Enabled && TelegramOidcOptions.IsValid(options.Value);
    public Result<string> CreateAuthorizationUrl(string state, string nonce, string codeVerifier) {
        if (!options.Value.Enabled || !TelegramOidcOptions.IsValid(options.Value)) {
            return Result.Failure<string>(TelegramIdentityErrors.NotConfigured);
        }
        string challenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
        string url = QueryHelpers.AddQueryString("https://oauth.telegram.org/auth", new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["client_id"] = options.Value.ClientId,
            ["redirect_uri"] = options.Value.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = "openid profile",
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
        });
        return Result.Success(url);
    }

    public async Task<Result<TelegramOidcIdentity>> ExchangeAsync(string code, string codeVerifier, string nonce, CancellationToken cancellationToken) {
        if (!options.Value.Enabled || !TelegramOidcOptions.IsValid(options.Value)) {
            return Result.Failure<TelegramOidcIdentity>(TelegramIdentityErrors.NotConfigured);
        }
        if (string.IsNullOrWhiteSpace(code) || code.Length > 4096 || codeVerifier.Length != 43 || string.IsNullOrWhiteSpace(nonce)) {
            return Invalid();
        }
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth.telegram.org/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(options.Value.ClientId + ":" + options.Value.ClientSecret)));
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal) {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = options.Value.RedirectUri,
            ["client_id"] = options.Value.ClientId,
            ["code_verifier"] = codeVerifier,
        });
        try {
            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) {
                return Invalid();
            }
            await response.Content.LoadIntoBufferAsync(32768, cancellationToken).ConfigureAwait(false);
            Stream content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (content.ConfigureAwait(false)) {
                using JsonDocument document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (!document.RootElement.TryGetProperty("id_token", out JsonElement idToken) || idToken.ValueKind != JsonValueKind.String) {
                    return Invalid();
                }
                return await tokens.ValidateAsync(idToken.GetString()!, nonce, cancellationToken).ConfigureAwait(false);
            }
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or OperationCanceledException) {
            return Invalid();
        }
    }

    private static Result<TelegramOidcIdentity> Invalid() => Result.Failure<TelegramOidcIdentity>(TelegramIdentityErrors.InvalidProof);
}
