using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Admin.Common;

public static class AdminMailInboxErrors {
    public static Error MessageNotFound(Guid id) => new(
        "MailInbox.MessageNotFound",
        $"Mail inbox message with ID {id} was not found.",
        Kind: ErrorKind.NotFound);
}
