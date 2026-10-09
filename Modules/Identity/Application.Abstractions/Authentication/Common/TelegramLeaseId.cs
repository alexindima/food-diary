namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

public readonly record struct TelegramLeaseId(Guid Value) {
    public static TelegramLeaseId New() => new(Guid.NewGuid());
    public static TelegramLeaseId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
