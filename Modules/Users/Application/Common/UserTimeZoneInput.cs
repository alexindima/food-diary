namespace FoodDiary.Application.Users.Common;

internal static class UserTimeZoneInput {
    internal static bool IsValid(string? value) {
        if (value is null) {
            return true;
        }
        if (string.IsNullOrWhiteSpace(value) || value.Length > 100) {
            return false;
        }
        try {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(value.Trim());
            return zone.HasIanaId || string.Equals(zone.Id, "UTC", StringComparison.Ordinal);
        } catch (TimeZoneNotFoundException) {
            return false;
        } catch (InvalidTimeZoneException) {
            return false;
        }
    }
}
