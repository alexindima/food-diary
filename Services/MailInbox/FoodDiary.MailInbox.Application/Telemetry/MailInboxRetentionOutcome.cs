namespace FoodDiary.MailInbox.Application.Telemetry;

public enum MailInboxRetentionOutcome {
    Failure = 0,
    ContentPurged = 1,
    MetadataDeleted = 2,
}
