namespace FoodDiary.MailRelay.Domain.Common;

public interface IEntityId<out T>
    where T : notnull {
    T Value { get; }
}
