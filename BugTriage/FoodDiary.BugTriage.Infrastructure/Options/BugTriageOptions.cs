namespace FoodDiary.BugTriage.Infrastructure.Options;

public sealed class BugTriageOptions {
    public string Recipient { get; set; } = "bugs@fooddiary.club";
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan ImportTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan ContentRetention { get; set; } = TimeSpan.FromDays(30);
}
