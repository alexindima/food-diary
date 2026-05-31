using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Wearables.Common;

public interface IWearableOAuthStateService {
    string CreateState(UserId userId, WearableProvider provider, string? clientState);

    bool IsValidState(string state, UserId userId, WearableProvider provider);
}
