using FoodDiary.Application.Abstractions.Admin.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class MailInbox {
        public static Error MessageNotFound(Guid id) => AdminMailInboxErrors.MessageNotFound(id);
    }
}
