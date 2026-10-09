namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

public readonly record struct TelegramOperationId(Guid Value) {
    public static TelegramOperationId New() => new(Guid.NewGuid());
    public static TelegramOperationId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
