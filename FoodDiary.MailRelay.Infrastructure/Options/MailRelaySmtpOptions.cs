namespace FoodDiary.MailRelay.Infrastructure.Options;

public sealed class MailRelaySmtpOptions {
    public const string SectionName = "RelaySmtp";

    public string Host { get; init; } = "smtp.example.com";
    public int Port { get; init; } = 587;
    public bool UseSsl { get; init; } = true;
    public string User { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
