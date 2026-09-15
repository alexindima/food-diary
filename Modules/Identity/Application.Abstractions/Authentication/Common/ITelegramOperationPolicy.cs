namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

public interface ITelegramOperationPolicy {
    bool OperationsEnabled { get; }
    long BotId { get; }
}
