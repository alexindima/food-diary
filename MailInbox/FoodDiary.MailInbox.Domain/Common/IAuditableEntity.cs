namespace FoodDiary.MailInbox.Domain.Common;

public interface IAuditableEntity {
    DateTime CreatedOnUtc { get; }
    DateTime? ModifiedOnUtc { get; }
}
