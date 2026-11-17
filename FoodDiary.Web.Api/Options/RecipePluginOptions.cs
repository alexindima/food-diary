namespace FoodDiary.Web.Api.Options;

internal sealed class RecipePluginOptions {
    public const string SectionName = "RecipePlugin";
    public bool Enabled { get; init; }
    public Uri? Issuer { get; init; }
    public Uri? WebOrigin { get; init; }
    public string ClientId { get; init; } = "fooddiary-chatgpt";
    public Uri? RedirectUri { get; init; }
    public string? CodexClientId { get; init; }
    public Uri? CodexRedirectUri { get; init; }
    public string? SigningCertificatePath { get; init; }
    public string? SigningCertificatePassword { get; init; }
    public string? EncryptionCertificatePath { get; init; }
    public string? EncryptionCertificatePassword { get; init; }

    public static RecipePluginOptions? ReadEnabled(IConfiguration configuration, bool development) {
        RecipePluginOptions? options = configuration.GetSection(SectionName).Get<RecipePluginOptions>();
        if (options?.Enabled != true) {
            return null;
        }
        if (!IsWebUri(options.Issuer, development) || !IsWebUri(options.WebOrigin, development) ||
            !IsWebUri(options.RedirectUri, development) ||
            options.Issuer!.AbsolutePath is not "/" || options.WebOrigin!.AbsolutePath is not "/" ||
            string.IsNullOrWhiteSpace(options.ClientId)) {
            throw new InvalidOperationException("RecipePlugin requires valid issuer, web origin, client ID and callback settings.");
        }
        if (!development && (string.IsNullOrWhiteSpace(options.SigningCertificatePath) ||
            string.IsNullOrWhiteSpace(options.EncryptionCertificatePath))) {
            throw new InvalidOperationException("RecipePlugin requires signing and encryption certificates.");
        }
        if ((options.CodexClientId is not null || options.CodexRedirectUri is not null) &&
            (string.IsNullOrWhiteSpace(options.CodexClientId) ||
             string.Equals(options.CodexClientId, options.ClientId, StringComparison.Ordinal) ||
            !IsWebUri(options.CodexRedirectUri, allowHttpLoopback: true) ||
             options.CodexRedirectUri is not { IsLoopback: true, Scheme: "http", Port: > 1023 })) {
            throw new InvalidOperationException("The Codex client requires a separate ID and a fixed HTTP loopback callback.");
        }
        return options;
    }

    private static bool IsWebUri(Uri? uri, bool allowHttpLoopback) =>
        uri is { IsAbsoluteUri: true, UserInfo.Length: 0, Query.Length: 0, Fragment.Length: 0 } &&
        (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) ||
         (allowHttpLoopback && uri.IsLoopback && string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal)));
}
