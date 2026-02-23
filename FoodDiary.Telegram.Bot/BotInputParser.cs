namespace FoodDiary.Telegram.Bot;

internal static class BotInputParser {
    private const string WaterPrefix = "water:";

    internal static bool TryParseWaterAmount(string? callbackData, out int amountMl) {
        amountMl = 0;
        if (string.IsNullOrWhiteSpace(callbackData) ||
            !callbackData.StartsWith(WaterPrefix, StringComparison.Ordinal)) {
            return false;
        }

        var amountText = callbackData[WaterPrefix.Length..];
        if (int.TryParse(amountText, out amountMl) && amountMl > 0) return true;
        amountMl = 0;
        return false;
    }
}
