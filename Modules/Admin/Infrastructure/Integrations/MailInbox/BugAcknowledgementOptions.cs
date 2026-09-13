namespace FoodDiary.Modules.Admin.Infrastructure.Integrations.MailInbox;

public sealed class BugAcknowledgementOptions {
    public bool Enabled { get; set; }
    public DateTimeOffset? StartAtUtc { get; set; }
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(5);
}
