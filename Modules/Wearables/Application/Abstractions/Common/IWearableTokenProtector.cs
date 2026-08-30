using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Abstractions.Wearables.Common;

public interface IWearableTokenProtector {
    ProtectedWearableToken Protect(string token);
    string Unprotect(ProtectedWearableToken protectedToken);
}
