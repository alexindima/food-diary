namespace FoodDiary.Domain.Common;

public interface IDomainEvent {
    DateTime OccurredOnUtc { get; }
}
