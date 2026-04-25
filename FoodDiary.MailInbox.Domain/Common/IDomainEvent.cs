namespace FoodDiary.MailInbox.Domain.Common;

public interface IDomainEvent {
    DateTime OccurredOnUtc { get; }
}
