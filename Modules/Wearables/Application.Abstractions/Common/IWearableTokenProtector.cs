using FoodDiary.Modules.Wearables.Domain.ValueObjects;

namespace FoodDiary.Modules.Wearables.Application.Abstractions.Common;

public interface IWearableTokenProtector {
    ProtectedWearableToken Protect(string token);
    string Unprotect(ProtectedWearableToken protectedToken);
}
