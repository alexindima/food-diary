namespace FoodDiary.Infrastructure.Options;

public sealed class WebPushOptions {
    public const string SectionName = "WebPush";

    public bool Enabled { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string DefaultUrl { get; set; } = "/";

    public static bool HasValidConfiguration(WebPushOptions options) {
        if (!options.Enabled) {
            return true;
        }

        return !string.IsNullOrWhiteSpace(options.Subject)
               && !string.IsNullOrWhiteSpace(options.PublicKey)
               && !string.IsNullOrWhiteSpace(options.PrivateKey)
               && options.Subject.Length <= 256
               && Uri.IsWellFormedUriString(options.Subject, UriKind.Absolute)
               && options.DefaultUrl.Length > 0
               && options.DefaultUrl.Length <= 256;
    }
}
