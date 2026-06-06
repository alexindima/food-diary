namespace FoodDiary.MailInbox.Application.Common.Results;

public static class MailInboxErrors {
    public static MailInboxError MessageNotFound(Guid id) =>
        new(
            "MailInbox.Message.NotFound",
            $"Inbound mail message '{id}' was not found.",
            ErrorKind.NotFound);
}
