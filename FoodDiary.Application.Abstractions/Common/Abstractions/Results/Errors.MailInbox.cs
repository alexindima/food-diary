namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class MailInbox {
        public static Error MessageNotFound(Guid id) => new(
            "MailInbox.MessageNotFound",
            $"Mail inbox message with ID {id} was not found.",
            Kind: ErrorKind.NotFound);
    }
}
