namespace FoodDiary.MailRelay.Infrastructure.Options;

public sealed class DirectMxOptions {
    public const string SectionName = "DirectMx";

    public int Port { get; init; } = 25;
    public int ConnectTimeoutSeconds { get; init; } = 20;
    public bool UseStartTlsWhenAvailable { get; init; } = true;

    public static bool HasValidConfiguration(DirectMxOptions options) {
        return options.Port > 0 && options.ConnectTimeoutSeconds > 0;
    }
}
