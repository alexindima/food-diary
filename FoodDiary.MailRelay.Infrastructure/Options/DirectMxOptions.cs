namespace FoodDiary.MailRelay.Infrastructure.Options;

public sealed class DirectMxOptions {
    public const string SectionName = "DirectMx";

    public int Port { get; init; } = 25;
    public int ConnectTimeoutSeconds { get; init; } = 20;
    public bool UseStartTlsWhenAvailable { get; init; } = true;
    public string LocalDomain { get; init; } = string.Empty;

    public static bool HasValidConfiguration(DirectMxOptions options) {
        return options.Port > 0 &&
               options.ConnectTimeoutSeconds > 0 &&
               HasValidLocalDomain(options.LocalDomain);
    }

    private static bool HasValidLocalDomain(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return true;
        }

        var localDomain = value.Trim();
        if (localDomain.Length > 253 || Uri.CheckHostName(localDomain) is not UriHostNameType.Dns) {
            return false;
        }

        var labels = localDomain.Split('.');
        return labels.Length > 1 &&
               labels.All(static label =>
                   label.Length is > 0 and <= 63 &&
                   char.IsLetterOrDigit(label[0]) &&
                   char.IsLetterOrDigit(label[^1]) &&
                   label.All(static c => char.IsLetterOrDigit(c) || c == '-'));
    }
}
