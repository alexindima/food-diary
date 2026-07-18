namespace FoodDiary.Application.Abstractions.Wearables.Common;

public interface IWearableTokenProtector {
    bool IsProtected(string token);
    string Protect(string token);
    string Unprotect(string protectedToken);
}
