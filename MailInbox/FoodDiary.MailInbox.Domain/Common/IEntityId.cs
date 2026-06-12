namespace FoodDiary.MailInbox.Domain.Common;

public interface IEntityId<out T> where T : notnull {
    T Value { get; }
}
