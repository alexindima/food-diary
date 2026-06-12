namespace FoodDiary.MailRelay.Domain.Common;

public interface IDomainEvent {
    DateTimeOffset OccurredOnUtc { get; }
}
