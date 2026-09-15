using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Wearables.Domain.Enums;

namespace FoodDiary.Modules.Wearables.Application.Common;

internal static class WearableConnectionLock {
    public static string Key(UserId userId, WearableProvider provider) =>
        $"wearable-connection:{userId.Value:N}:{provider}";
}
