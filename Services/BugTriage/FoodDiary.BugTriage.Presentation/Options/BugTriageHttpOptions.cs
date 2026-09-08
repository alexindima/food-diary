namespace FoodDiary.BugTriage.Presentation.Options;

public sealed class BugTriageHttpOptions {
    public string ApiKey { get; set; } = string.Empty;
    public string ReadApiKey { get; set; } = string.Empty;
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(30);
    public int MaxAttempts { get; set; } = 3;
}
