using FoodDiary.Modules.Wearables.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Wearables.Application.Abstractions.Common;

public interface IWearableOAuthStateService {
    string CreateState(UserId userId, WearableProvider provider, string? clientState);

    bool IsValidState(string state, UserId userId, WearableProvider provider);
}
